using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Roslyn.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Analyzer.Roslyn
{
    public sealed class RoslynCodeAnalyzer : ICodeAnalyzer
    {
        const string CSharpFilePattern = "*.cs";
        private readonly List<IRule> _rules = new()
        {
            new WeakHashingRule(),
            new HardCodedSecretRule()
        };

        public IReadOnlyCollection<Finding> AnalyzeDirectory(string path)
        {
            var findings = new List<Finding>();

            var files = Directory.GetFiles(path, CSharpFilePattern, SearchOption.AllDirectories);
            var syntaxTree = files.Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f)).ToList();
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Security.Cryptography.MD5).Assembly.Location)
            };
            var compilation = CSharpCompilation.Create("Analysis", syntaxTree, references, 
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            
            foreach (var tree in syntaxTree)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var root = tree.GetRoot();
                var filePath = tree.FilePath;

                foreach(var rule in _rules)
                {
                    findings.AddRange(rule.AnalyzeWithSemanticModel(root, semanticModel, filePath));
                }
            }
            return findings;
        }
    }
}
