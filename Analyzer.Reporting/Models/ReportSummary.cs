using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Reporting.Models
{
    public sealed class ReportSummary
    {
        public int TotalFindings { get; init; }
        public int Critical { get; init; }
        public int High { get; init; }
        public int Medium { get; init; }
        public int Low { get; init; }
        public int Info { get; init; }
    }
}
