using Analyzer.Core.Models;
using Microsoft.CodeAnalysis;

namespace Analyzer.Core.Interfaces
{
    public interface IRule
    {
        string Id { get; }
        Vulnerability Vulnerability { get; }
        IEnumerable<Finding> AnalyzeWithSemanticModel(SyntaxNode root, SemanticModel semanticModel, string filePath);
    }
}
