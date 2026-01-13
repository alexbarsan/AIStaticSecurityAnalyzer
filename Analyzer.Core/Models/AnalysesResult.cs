using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Core.Models
{
    public sealed class AnalysesResult
    {
        public IReadOnlyCollection<Finding> Findings { get; init; } = Array.Empty<Finding>();
        public Severity MaxSeverity => Findings.Any() ? Findings.Max(f => f.Vulnerability.Severity) : Severity.Info;
    }
}
