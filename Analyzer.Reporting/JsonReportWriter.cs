using Analyzer.Core.Models;
using Analyzer.Reporting.Models;
using System.Text.Json;

namespace Analyzer.Reporting;

public sealed class JsonReportWriter
{
    public void Write(string outputPath, string projectPath, IReadOnlyCollection<Finding> findings)
    {
        var summary = new ReportSummary
        {
            TotalFindings = findings.Count,
            Critical = findings.Count(f => f.Vulnerability.Severity == Severity.Critical),
            High = findings.Count(f => f.Vulnerability.Severity == Severity.High),
            Medium = findings.Count(f => f.Vulnerability.Severity == Severity.Medium),
            Low = findings.Count(f => f.Vulnerability.Severity == Severity.Low),
            Info = findings.Count(f => f.Vulnerability.Severity == Severity.Info)
        };

        var report = new AnalysisReport
        {
            ProjectPath = projectPath,
            Summary = summary,
            Findings = findings
        };

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(outputPath, json);
    }
}
