using System.Text;
using Analyzer.Core.Models;
using Analyzer.Core.Training;

namespace Analyzer.Reporting;

public sealed class CsvTrainingExporter
{
    public void Append(string path, IReadOnlyCollection<Finding> findings)
    {
        var sb = new StringBuilder();

        // Candidate exports are intentionally unlabeled. They are reviewed and
        // later copied into the canonical labeled dataset after manual labeling.
        if (!File.Exists(path))
        {
            sb.AppendLine(TrainingCsvSchema.CandidateHeader);
        }

        foreach (var f in findings)
        {
            var snippet = f.CodeSnippet ?? "";
            var jwt = (snippet.Count(c => c == '.') >= 2 && snippet.Length >= 20) ? 1 : 0;
            var url = snippet.Contains("http", StringComparison.OrdinalIgnoreCase) || snippet.Contains("localhost", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            var base64ish = (snippet.Length >= 20 && snippet.All(c => char.IsLetterOrDigit(c) || c is '+' or '/' or '=' or '-' or '_')) ? 1 : 0;
            var placeholder = snippet.Contains("changeme", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            sb.AppendLine(string.Join(",",
                Escape(f.Vulnerability.Id),
                Escape(f.Vulnerability.CWEId),
                snippet.Length,
                jwt,
                base64ish,
                url,
                placeholder));
        }

        File.AppendAllText(path, sb.ToString());
    }

    private static string Escape(string? s)
    {
        s ??= "";
        if (s.Contains(',') || s.Contains('"'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }
}
