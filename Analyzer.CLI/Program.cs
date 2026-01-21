// See https://aka.ms/new-console-template for more information
using Analyzer.Core.Models;
using Analyzer.Roslyn;

Console.WriteLine("AI Static Security Analyzer - CLI!");

if(args.Length == 0)
{
    Console.WriteLine("Usage: analyzer <path>");
    return;
}

var path = args[0];
var analyzer = new RoslynCodeAnalyzer();
var findings = analyzer.AnalyzeDirectory(path);
foreach (var finding in findings)
{
    Console.WriteLine($"[{finding.Vulnerability.Severity}] {finding.Vulnerability.Name} at {finding.FilePath} : {finding.Line}");
}
Environment.Exit(findings.Any(f => f.Vulnerability.Severity >= Severity.High) ? 2 : 0);