// See https://aka.ms/new-console-template for more information
using Analyzer.Core.Models;
using Analyzer.Reporting;
using Analyzer.Roslyn;

Console.WriteLine("AI Static Security Analyzer - CLI!");

var jsonParam = "--json";
if(args.Length == 0)
{
    Console.WriteLine($"Usage: analyzer <path> [{jsonParam}]");
    return;
}

var path = args[0];
var exportJson = args.Contains(jsonParam);
var analyzer = new RoslynCodeAnalyzer();
var findings = analyzer.AnalyzeDirectory(path);
foreach (var finding in findings)
{
    Console.WriteLine($"[{finding.Vulnerability.Severity}] {finding.Vulnerability.Name} at {finding.FilePath} : {finding.Line}");
}

if (exportJson)
{
    var reportName = "analysis-report.json";
    var writer = new JsonReportWriter();
    writer.Write(reportName, path, findings);
    Console.WriteLine($"JSON report written to {reportName}");
}

//CI/CD logic
var maxSeverity = findings.Any() ? findings.Max(f => f.Vulnerability.Severity) : Severity.Info;

Environment.Exit(maxSeverity >= Severity.High ? 2 : 0);