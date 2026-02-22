using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.AI.Models
{
    public sealed class AiInput
    {
        // Label: 1 = true positive, 0 = false positive
        [LoadColumn(0)]
        public bool Label { get; set; }
        // Features
        [LoadColumn(1)]
        public string RuleId { get; set; } = string.Empty;
        [LoadColumn(2)]
        public string CweId { get; set; } = string.Empty;
        [LoadColumn(3)]
        public float SnippetLength { get; set; }
        [LoadColumn(4)]
        public float HasJwtShape { get; set; }          // 0/1
        [LoadColumn(5)]
        public float HasBase64Shape { get; set; }       // 0/1
        [LoadColumn(6)]
        public float HasUrlShape { get; set; }          // 0/1
        [LoadColumn(7)]
        public float HasPlaceholderValue { get; set; }  // 0/1
    }
}
