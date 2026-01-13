using Analyzer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Core.Interfaces
{
    public interface IRule
    {
        string Id { get; }
        Vulnerability Vulnerability { get; }
        IEnumerable<Finding> Analyze(string sourceCode, string filePath);
    }
}
