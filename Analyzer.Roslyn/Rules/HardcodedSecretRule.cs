using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyzer.Roslyn.Rules;

public sealed class HardCodedSecretRule : IRule
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

    private static readonly string[] SensitiveInformation = { "password", "pwd", "secret", "token", "apiKey", "api_key" };

    public IEnumerable<Finding> AnalyzeWithSemanticModel(
        SyntaxNode root,
        SemanticModel semanticModel,
        string filePath)
    {
        foreach (var f in AnalyzeVariableDeclarators(root, semanticModel, filePath))
            yield return f;

        foreach (var f in AnalyzePropertyInitializers(root, semanticModel, filePath))
            yield return f;

        foreach (var f in AnalyzeAssignments(root, semanticModel, filePath))
            yield return f;
    }

    private IEnumerable<Finding> AnalyzeVariableDeclarators(SyntaxNode root, SemanticModel semanticModel, string filePath)
    {
        // Covers:
        // - local variables: var token = "..."
        // - fields: private string apiKey = "..."
        // - const locals/fields: const string ApiKey = "..."
        var declarators = root.DescendantNodes().OfType<VariableDeclaratorSyntax>();

        foreach (var d in declarators)
        {
            if (d.Initializer?.Value is not LiteralExpressionSyntax literal ||
                !literal.IsKind(SyntaxKind.StringLiteralExpression))
                continue;

            var symbol = semanticModel.GetDeclaredSymbol(d);
            if (symbol == null)
                continue;

            if (symbol is not ILocalSymbol &&
                symbol is not IFieldSymbol)
                continue;

            var name = symbol.Name;
            if (!IsSensitiveName(name))
                continue;

            var value = literal.Token.ValueText;
            if (!IsLikelySecret(value))
                continue;

            yield return CreateFinding(literal, filePath, d.ToString());
        }
    }

    private IEnumerable<Finding> AnalyzePropertyInitializers(SyntaxNode root, SemanticModel semanticModel, string filePath)
    {
        // Covers:
        // public string Token { get; } = "..."
        var props = root.DescendantNodes().OfType<PropertyDeclarationSyntax>();

        foreach (var p in props)
        {
            if (p.Initializer?.Value is not LiteralExpressionSyntax literal ||
                !literal.IsKind(SyntaxKind.StringLiteralExpression))
                continue;

            var symbol = semanticModel.GetDeclaredSymbol(p);
            if (symbol == null)
                continue;

            if (symbol is not IPropertySymbol propSymbol)
                continue;

            if (!IsSensitiveName(propSymbol.Name))
                continue;

            var value = literal.Token.ValueText;
            if (!IsLikelySecret(value))
                continue;

            yield return CreateFinding(literal, filePath, p.ToString());
        }
    }

    private IEnumerable<Finding> AnalyzeAssignments(SyntaxNode root, SemanticModel semanticModel, string filePath)
    {
        // Covers:
        // token = "..."
        // this.apiKey = "..."
        // _password = "..."
        var assignments = root.DescendantNodes().OfType<AssignmentExpressionSyntax>();

        foreach (var a in assignments)
        {
            if (a.Right is not LiteralExpressionSyntax literal ||
                !literal.IsKind(SyntaxKind.StringLiteralExpression))
                continue;

            // We want the *symbol* of the left side (identifier/field/property)
            var leftSymbol = semanticModel.GetSymbolInfo(a.Left).Symbol;
            if (leftSymbol == null)
                continue;

            string leftName = leftSymbol.Name;

            // Only consider locals/fields/properties
            if (leftSymbol is not ILocalSymbol &&
                leftSymbol is not IFieldSymbol &&
                leftSymbol is not IPropertySymbol)
                continue;

            if (!IsSensitiveName(leftName))
                continue;

            var value = literal.Token.ValueText;
            if (!IsLikelySecret(value))
                continue;

            yield return CreateFinding(literal, filePath, a.ToString());
        }
    }

    private static bool IsSensitiveName(string name)
    {
        var n = name.ToLowerInvariant();
        return SensitiveInformation.Any(part => n.Contains(part));
    }

    private static bool IsLikelySecret(string value)
    {
        // MVP heuristics: simple but effective.
        // (1) ignore tiny strings
        if (value.Length < 8)
            return false;

        // (2) ignore obviously non-secret human text
        if (value.Contains(' ') || value.Contains('\t') || value.Contains('\n'))
            return false;

        // (3) detect common token shapes (JWT, API-key-ish)
        // JWT usually contains two dots
        if (value.Count(c => c == '.') >= 2 && value.Length >= 20)
            return true;

        // Many keys are long and mixed (letters+digits)
        bool hasLetter = value.Any(char.IsLetter);
        bool hasDigit = value.Any(char.IsDigit);

        if (value.Length >= 20 && hasLetter && hasDigit)
            return true;

        // fallback: medium length + mixed characters is suspicious
        if (value.Length >= 12 && hasLetter && hasDigit)
            return true;

        return false;
    }

    private Finding CreateFinding(LiteralExpressionSyntax literal, string filePath, string codeSnippet)
    {
        var location = literal.GetLocation().GetLineSpan();

        return new Finding
        {
            Vulnerability = Vulnerability,
            FilePath = filePath,
            Line = location.StartLinePosition.Line + 1,
            Column = location.StartLinePosition.Character + 1,
            CodeSnippet = codeSnippet,
            Confidence = 1.0
        };
    }
}
