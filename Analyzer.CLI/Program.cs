using Analyzer.Core.Models;
using Analyzer.Reporting;
using Analyzer.Roslyn;

Console.WriteLine("AI Static Security Analyzer");

// Usage:
// analyzer <path> [--json] [--fail-on <info|low|medium|high|critical>]
if (args.Length == 0)
{
    PrintUsage();
    return;
}

var path = args[0];
var exportJson = args.Contains("--json");
var failOn = ParseFailOn(args) ?? Severity.High;

var analyzer = new RoslynCodeAnalyzer();
var findings = analyzer.AnalyzeDirectory(path);

foreach (var finding in findings)
{
    Console.WriteLine(
        $"[{finding.Vulnerability.Severity}] {finding.Vulnerability.Name} " +
        $"at {finding.FilePath}:{finding.Line}");
}

if (exportJson)
{
    var writer = new JsonReportWriter();
    writer.Write("analysis-report.json", path, findings);
    Console.WriteLine("JSON report written to analysis-report.json");
}

// CI/CD decision
var maxSeverity = findings.Any()
    ? findings.Max(f => f.Vulnerability.Severity)
    : Severity.Info;

var exitCode = maxSeverity >= failOn ? 2 : 0;
Environment.Exit(exitCode);

static void PrintUsage()
{
    Console.WriteLine("Usage: analyzer <path> [--json] [--fail-on <info|low|medium|high|critical>]");
    Console.WriteLine("Example: analyzer ./src --json --fail-on high");
}

static Severity? ParseFailOn(string[] args)
{
    var idx = Array.FindIndex(args, a => a.Equals("--fail-on", StringComparison.OrdinalIgnoreCase));
    if (idx < 0 || idx + 1 >= args.Length)
        return null;

    var raw = args[idx + 1].Trim().ToLowerInvariant();
    return raw switch
    {
        "info" => Severity.Info,
        "low" => Severity.Low,
        "medium" => Severity.Medium,
        "high" => Severity.High,
        "critical" => Severity.Critical,
        _ => throw new ArgumentException("Invalid --fail-on value. Use: info|low|medium|high|critical")
    };
}
