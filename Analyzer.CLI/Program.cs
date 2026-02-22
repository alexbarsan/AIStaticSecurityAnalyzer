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
        modelPath: "ai-model.zip");

    Console.WriteLine("AI model saved to ai-model.zip");
    return;
}

// default: scan
var path = args[0];
var exportJson = args.Contains("--json");
var useAi = args.Contains("--ai");

var failOn = ParseFailOn(args) ?? Severity.High;

var analyzer = new RoslynCodeAnalyzer();
var findings = analyzer.AnalyzeDirectory(path);

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
    scorer.LoadModel("ai-model.zip");
    scorer.ScoreFindings(findings);
}

if (exportJson)
{
    var writer = new JsonReportWriter();
    writer.Write("analysis-report.json", path, findings);
    Console.WriteLine("JSON report written to analysis-report.json");
}


var maxSeverity = findings.Count != 0 ? findings.Max(f => f.Vulnerability.Severity) : Severity.Info;
Environment.Exit(maxSeverity >= failOn ? 2 : 0);

static int? ParseIntArg(string[] args, string name)
{
    var idx = Array.FindIndex(args, a => a.Equals(name, StringComparison.OrdinalIgnoreCase));
    if (idx < 0 || idx + 1 >= args.Length) return null;
    return int.TryParse(args[idx + 1], out var v) ? v : null;
}

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  analyzer <path> [--json] [--fail-on <info|low|medium|high|critical>]");
    Console.WriteLine("  analyzer sync-nvd --days <n>");
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
