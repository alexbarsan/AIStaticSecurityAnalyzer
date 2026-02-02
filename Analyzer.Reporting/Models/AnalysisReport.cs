using Analyzer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Reporting.Models
{
    public sealed class AnalysisReport
    {
        public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
        public string ProjectPath { get; init; } = string.Empty;

        public ReportSummary Summary { get; init; } = new();
        public IReadOnlyCollection<Finding> Findings { get; init; }
            = Array.Empty<Finding>();
    }
}
