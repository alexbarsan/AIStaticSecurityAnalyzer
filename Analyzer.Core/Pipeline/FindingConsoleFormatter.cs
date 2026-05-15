using Analyzer.Core.Models;

namespace Analyzer.Core.Pipeline;

public static class FindingConsoleFormatter
{
    public static string Format(Finding finding)
    {
        var cveText = string.IsNullOrWhiteSpace(finding.CveId)
            ? string.Empty
            : $" ({finding.CveId}, CVSS {finding.CvssBaseScore?.ToString("0.0") ?? "?"})";

        return $"[{finding.Vulnerability.Severity}] {finding.Vulnerability.Name} (conf {finding.Confidence:0.00}) {cveText} at {finding.FilePath}:{finding.Line}";
    }
}
