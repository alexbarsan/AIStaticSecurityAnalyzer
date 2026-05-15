using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Roslyn.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Analyzer.Roslyn
{
    public sealed class RoslynCodeAnalyzer : ICodeAnalyzer
    {
        private readonly List<IRule> _rules = new()
        {
            new WeakHashingRule(),
            new HardCodedSecretRule()
        };

        public IReadOnlyCollection<Finding> AnalyzePath(string path)
        {
            var sourceFiles = AnalysisInputResolver.ResolveSourceFiles(path);
            var syntaxTrees = sourceFiles
                .Select(filePath => CSharpSyntaxTree.ParseText(File.ReadAllText(filePath), path: filePath))
                .ToList();

            var compilation = CSharpCompilation.Create(
                AnalysisInputResolver.GetCompilationName(path),
                syntaxTrees,
                CreateMetadataReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return AnalyzeCompilation(compilation);
        }

        public IReadOnlyCollection<Finding> AnalyzeDirectory(string path) => AnalyzePath(path);

        private IReadOnlyCollection<Finding> AnalyzeCompilation(Compilation compilation)
        {
            var findings = new List<Finding>();
            var syntaxTrees = compilation.SyntaxTrees
                .Where(tree => !string.IsNullOrWhiteSpace(tree.FilePath) && AnalysisInputResolver.ShouldIncludeSourceFile(tree.FilePath))
                .OrderBy(tree => tree.FilePath, StringComparer.OrdinalIgnoreCase);

            foreach (var tree in syntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var root = tree.GetRoot();
                var filePath = tree.FilePath;

                foreach (var rule in _rules)
                {
                    findings.AddRange(rule.AnalyzeWithSemanticModel(root, semanticModel, filePath));
                }
            }

            return OrderFindings(findings);
        }

        private static IReadOnlyCollection<Finding> OrderFindings(IEnumerable<Finding> findings) =>
            findings
                .OrderBy(f => f.FilePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(f => f.Line)
                .ThenBy(f => f.Column)
                .ThenBy(f => f.Vulnerability.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();

        private static IReadOnlyCollection<MetadataReference> CreateMetadataReferences()
        {
            var trustedPlatformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
            if (!string.IsNullOrWhiteSpace(trustedPlatformAssemblies))
            {
                return trustedPlatformAssemblies
                    .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .Select(path => MetadataReference.CreateFromFile(path))
                    .ToList();
            }

            return
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Security.Cryptography.MD5).Assembly.Location)
            ];
        }
    }
}
