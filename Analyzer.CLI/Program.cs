using Analyzer.Core.Models;
using Analyzer.CVE.Enrichment;
using Analyzer.CVE.Nvd;
using Analyzer.CVE.Storage;
using Analyzer.Reporting;
using Analyzer.Roslyn;
using Analyzer.AI.Training;


Console.WriteLine("AI Static Security Analyzer");

if (args.Length == 0)
{
    PrintUsage();
    return;
}

var mode = args[0].ToLowerInvariant();
var dbPath = "cves.db";

if (mode == "sync-nvd")
{
    var days = ParseIntArg(args, "--days") ?? 7;

    DbInitializer.EnsureCreated(dbPath);

    var apiKey = Environment.GetEnvironmentVariable("NVD_API_KEY");
    using var http = new HttpClient();

    var client = new NvdClient(http, apiKey);
    var repo = new CveRepository(dbPath);
    var importer = new NvdImporter(client, repo);

    var end = DateTimeOffset.UtcNow;
    var start = end.AddDays(-days);

    Console.WriteLine($"Syncing NVD modified CVEs from {start:u} to {end:u}...");
    await importer.SyncModifiedWindowAsync(start, end, CancellationToken.None);
    Console.WriteLine("Done.");
    return;
}

string modelPath = "ai-model.zip";

if (mode == "train-ai")
{
    var csvPath = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory, "..", "..", "..", "..", "Analyzer.AI", "Training", "training-data.csv"));

    if (!File.Exists(csvPath))
    {
        Console.WriteLine($"Training CSV not found: {csvPath}");
        return;
    }
    TrainModel.Train(
        csvPath,
        modelPath: modelPath);

    Console.WriteLine($"AI model saved to {modelPath}");
    return;
}

// default: scan
var path = args[0];
var exportJson = args.Contains("--json");
var useAi = args.Contains("--ai");
var sarifOutput = ParseStringArgWithDefault(args, "--sarif", "analysis.sarif.json");
var exportSarif = args.Contains("--sarif");

var failOn = ParseFailOn(args) ?? Severity.High;

var analyzer = new RoslynCodeAnalyzer();
var findings = analyzer.AnalyzeDirectory(path).ToList();

// Enrich findings (best-effort)
DbInitializer.EnsureCreated(dbPath);
var enricher = new FindingEnricher(new CveRepository(dbPath));
enricher.Enrich(findings);

foreach (var finding in findings)
{
    var cveText = string.IsNullOrWhiteSpace(finding.CveId) ? "" : $" ({finding.CveId}, CVSS {finding.CvssBaseScore?.ToString("0.0") ?? "?"})";
    Console.WriteLine($"[{finding.Vulnerability.Severity}] {finding.Vulnerability.Name} (conf {finding.Confidence:0.00}) {cveText} at {finding.FilePath}:{finding.Line}");
}

if (useAi)
{
    var scorer = new Analyzer.AI.AiScorer();
    scorer.LoadModel(modelPath);
    scorer.ScoreFindings(findings);
}
var minConfidence = ParseDoubleArg(args, "--min-confidence") ?? 0.0;

if (useAi && minConfidence > 0.0)
{
    findings = findings
        .Where(f => f.Confidence >= minConfidence)
        .ToList();
}

var exportTrainingPath = ParseStringArg(args, "--export-training");
if (!string.IsNullOrWhiteSpace(exportTrainingPath))
{
    string finalPath;

    if (Path.IsPathRooted(exportTrainingPath))
    {
        finalPath = exportTrainingPath;
    }
    else
    {
        var trainingDir = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Analyzer.AI",
            "Training");

        Directory.CreateDirectory(trainingDir);

        finalPath = Path.Combine(trainingDir, exportTrainingPath);
    }

    var exporter = new Analyzer.Reporting.CsvTrainingExporter();
    exporter.Append(finalPath, findings);

    Console.WriteLine($"Training data exported to {finalPath}");
}



if (exportJson)
{
    var writer = new JsonReportWriter();
    writer.Write("analysis-report.json", path, findings);
    Console.WriteLine("JSON report written to analysis-report.json");
}

if (exportSarif)
{
    var sarifWriter = new Analyzer.Reporting.Sarif.SarifReportWriter();
    sarifWriter.Write(sarifOutput!, path, findings);
    Console.WriteLine($"SARIF report written to {Path.GetFullPath(sarifOutput!)}");
}

var maxSeverity = findings.Count != 0 ? findings.Max(f => f.Vulnerability.Severity) : Severity.Info;
Environment.Exit(maxSeverity >= failOn ? 2 : 0);

static int? ParseIntArg(string[] args, string name)
{
    var idx = Array.FindIndex(args, a => a.Equals(name, StringComparison.OrdinalIgnoreCase));
    if (idx < 0 || idx + 1 >= args.Length) return null;
    return int.TryParse(args[idx + 1], out var v) ? v : null;
}

static double? ParseDoubleArg(string[] args, string name)
{
    var idx = Array.FindIndex(args, a => a.Equals(name, StringComparison.OrdinalIgnoreCase));
    if (idx < 0 || idx + 1 >= args.Length) return null;

    return double.TryParse(args[idx + 1], out var v) ? v : null;
}

static string? ParseStringArg(string[] args, string name)
{
    var idx = Array.FindIndex(args, a => a.Equals(name, StringComparison.OrdinalIgnoreCase));
    if (idx < 0 || idx + 1 >= args.Length) return null;

    return args[idx + 1];
}

static string? ParseStringArgWithDefault(string[] args, string name, string defaultValue)
{
    var idx = Array.FindIndex(args, a => a.Equals(name, StringComparison.OrdinalIgnoreCase));
    if (idx < 0) return null;

    if (idx + 1 >= args.Length || args[idx + 1].StartsWith("--"))
        return defaultValue;

    return args[idx + 1];
}

static Severity? ParseFailOn(string[] args)
{
    var idx = Array.FindIndex(args, a => a.Equals("--fail-on", StringComparison.OrdinalIgnoreCase));
    if (idx < 0 || idx + 1 >= args.Length) return null;

    return args[idx + 1].Trim().ToLowerInvariant() switch
    {
        "info" => Severity.Info,
        "low" => Severity.Low,
        "medium" => Severity.Medium,
        "high" => Severity.High,
        "critical" => Severity.Critical,
        _ => throw new ArgumentException("Invalid --fail-on value. Use: info|low|medium|high|critical")
    };
}

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  analyzer <path> [--json] [--fail-on <info|low|medium|high|critical>]");
    Console.WriteLine("  analyzer sync-nvd --days <n>");    
    Console.WriteLine("  --ai --min-confidence <0..1>  Enables AI scoring and filters low-confidence findings");
    Console.WriteLine("  --export-training <file.csv>  Export findings as ML training candidates");
    Console.WriteLine("  --sarif [file]  Export SARIF 2.1.0 report (default: analysis.sarif.json)");
}
