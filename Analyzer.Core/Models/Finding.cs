using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Core.Models
{
    public sealed class Finding
    {
        public Vulnerability Vulnerability { get; init; } = default!;
        public string FilePath { get; init; } = string.Empty;
        public int Line { get; init; }
        public int Column { get; init; }
        public string CodeSnippet { get; init; } = string.Empty;
        public double Confidence { get; set; } = 1.0; //default value, may be adjusted by AI later => to reduce false positive
        
    }
}
