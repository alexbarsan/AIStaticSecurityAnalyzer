using Analyzer.Core.Models;

namespace Analyzer.Core.Pipeline;

public static class ScanPipelineProcessor
{
    public static ScanPipelineResult Process(
        IReadOnlyCollection<Finding> findings,
        bool useAi,
        double minConfidence,
        Action<IReadOnlyList<Finding>>? scoreFindings)
    {
        var working = findings.ToList();

        if (useAi)
        {
            scoreFindings?.Invoke(working);
        }

        if (useAi && minConfidence > 0.0)
        {
            working = working
                .Where(f => f.Confidence >= minConfidence)
                .ToList();
        }

        var consoleLines = working
            .Select(FindingConsoleFormatter.Format)
            .ToList();

        return new ScanPipelineResult
        {
            FinalFindings = working,
            ConsoleLines = consoleLines
        };
    }
}
