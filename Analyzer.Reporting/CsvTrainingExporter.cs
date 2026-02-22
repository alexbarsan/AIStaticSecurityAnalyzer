using System.Text;
using Analyzer.Core.Models;

namespace Analyzer.Reporting;

public sealed class CsvTrainingExporter
{
    public void Append(string path, IReadOnlyCollection<Finding> findings)
    {
        var sb = new StringBuilder();

        // header only if file doesn't exist
        if (!File.Exists(path))
        {
            sb.AppendLine("Label,RuleId,CweId,SnippetLength,HasJwtShape,HasBase64Shape,HasUrlShape,HasPlaceholderValue");
        }

        foreach (var f in findings)
        {
            // Label left blank (-1) because you will manually label later
            // (you can change this scheme however you like)
            var snippet = f.CodeSnippet ?? "";
            var jwt = (snippet.Count(c => c == '.') >= 2 && snippet.Length >= 20) ? 1 : 0;
            var url = snippet.Contains("http", StringComparison.OrdinalIgnoreCase) || snippet.Contains("localhost", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            var base64ish = (snippet.Length >= 20 && snippet.All(c => char.IsLetterOrDigit(c) || c is '+' or '/' or '=' or '-' or '_')) ? 1 : 0;
            var placeholder = snippet.Contains("changeme", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            sb.AppendLine(string.Join(",",
                -1,
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
