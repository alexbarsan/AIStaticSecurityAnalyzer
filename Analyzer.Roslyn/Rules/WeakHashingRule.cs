using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Roslyn.Rules
{
    public class WeakHashingRule : IRule
    {
        public string Id => "RULE-CRYPTO-001";
        const string MD5 = "System.Security.Cryptography.MD5";
        const string SHA1 = "System.Security.Cryptography.SHA1";
        public Vulnerability Vulnerability { get; } = new()
        {
            Id = "VULN-WEAK-HASHING",
            Name = "Use of weak cryptographic hashing algorithm",
            Description = "The application uses MD5 or SHA1 which are considered cryptographically broken.",
            CWEId = "CWE-327",
            Severity = Severity.High,
            Recommandation = "Use SHA-256 or stronger hashing algorithms."
        };

        public IEnumerable<Finding> AnalyzeWithSemanticModel(SyntaxNode root, SemanticModel semanticModel, string filePath)
        {
            var invocations = root?.DescendantNodes()?.OfType<InvocationExpressionSyntax>();
            if (invocations != null)
            {
                foreach (var invocation in invocations)
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(invocation);
                    var methodSymbol = symbolInfo.Symbol as IMethodSymbol;

                    if (methodSymbol == null) continue;

                    var containingType = methodSymbol?.ContainingType?.ToString();
                    if (containingType !=null && (containingType.Contains(MD5) || containingType.Contains(SHA1)))
                    {
                        var location = invocation.GetLocation().GetLineSpan();
                        yield return new Finding
                        {
                            Vulnerability = Vulnerability,
                            FilePath = filePath,
                            Line = location.StartLinePosition.Line + 1,
                            Column = location.StartLinePosition.Character + 1,
                            CodeSnippet = invocation.ToString()
                        };
                    }
                }
            }
        }
    }
}
