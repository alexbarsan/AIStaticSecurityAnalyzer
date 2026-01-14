using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
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
        const string MD5 = "MD5";
        const string SHA1 = "SHA1";
        public Vulnerability Vulnerability { get; } = new()
        {
            Id = "VULN-WEAK-HASHING",
            Name = "Use of weak cryptographic hashing algorithm",
            Description = "The application uses MD5 or SHA1 which are considered cryptographically broken.",
            CWEId = "CWE-327",
            Severity = Severity.High,
            Recommandation = "Use SHA-256 or stronger hashing algorithms."
        };

        public IEnumerable<Finding> Analyze(string sourceCode, string filePath)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = syntaxTree.GetRoot();
            var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invocation in invocations) { 
                var expression = invocation.Expression.ToString();
                if (expression.Contains(MD5) || expression.Contains(SHA1))
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
