using Analyzer.Core.Models;

namespace Analyzer.Core.Execution;

public static class ExitCodePolicy
{
    public static int GetExitCode(Severity maxSeverity, Severity failOn, bool skipGate) =>
        skipGate ? 0 : maxSeverity >= failOn ? 2 : 0;

    public static bool ShouldSkipGateForExport(bool exportTraining, bool failOnSpecified) =>
        exportTraining && !failOnSpecified;
}
