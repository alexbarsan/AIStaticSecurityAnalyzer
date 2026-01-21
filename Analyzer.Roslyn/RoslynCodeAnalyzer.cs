using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Roslyn.Rules;

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
            foreach (var file in files)
            {
                var code = File.ReadAllText(file);
                foreach(var rule in _rules)
                {
                    findings.AddRange(rule.Analyze(code, file));
                }
            }
            return findings;
        }
    }
}
