using Analyzer.Core.Models;

namespace Analyzer.Core.Pipeline;

public sealed class ScanPipelineResult
{
    public IReadOnlyList<Finding> FinalFindings { get; init; } = Array.Empty<Finding>();
    public IReadOnlyList<string> ConsoleLines { get; init; } = Array.Empty<string>();
}
