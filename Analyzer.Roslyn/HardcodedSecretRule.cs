using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyzer.Roslyn;

public class HardCodedSecretRule : IRule
{
    public string Id => "RULE-SECRETS-001";

    public Vulnerability Vulnerability { get;  } = new()
    {
        Id = "VULN-HARDCODED-SECRET",
        Name = "Hardcoded secret in source code",
        Description = "Sensitive information: passwords, API keys or tokens are stored in the environment variables!",
        CWEId = "CWE-798",
        Severity = Severity.Critical,
        Recommandation = "Store secrets in Vaults, secure configuration systems or environment variables!"
    };

    private static readonly string[] SensitiveInformation = { "password", "pwd", "secret", "token", "apiKey", "api_key", "key" };

    public IEnumerable<Finding> Analyze(string sourceCode, string filePath)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetRoot();
        var variables = root.DescendantNodes().OfType<VariableDeclaratorSyntax>();
        foreach (var variable in variables)
        {
            var name = variable.Identifier.Text.ToLower();
            if(!SensitiveInformation.Any(x => name.Contains(x))) continue;
            if(variable.Initializer?.Value is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                var value = literal.Token.ValueText;
                //simple heurirstic; maybe I will change it later => ignore very small strings
                if (value.Length < 6) continue;

                var location = literal.GetLocation().GetLineSpan();
                yield return new Finding
                {
                    Vulnerability = Vulnerability,
                    FilePath = filePath,
                    Line = location.StartLinePosition.Line + 1,
                    Column = location.StartLinePosition.Character + 1,
                    CodeSnippet = variable.ToString()
                };
            }
        }
    }
}
