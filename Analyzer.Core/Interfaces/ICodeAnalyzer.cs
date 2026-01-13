using Analyzer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Core.Interfaces
{
    public interface ICodeAnalyzer
    {
        IReadOnlyCollection<Finding> AnalyzeDirectory(string path);
    }
}
