using Analyzer.AI.Training;
using Analyzer.Core.Execution;
using Analyzer.Core.Models;
using Analyzer.Reporting;
using Analyzer.Roslyn;

namespace Analyzer.Tests;

internal static class Program
{
    private static int Main(string[] args)
    {
        var tests = new (string Name, Action Test)[]
        {
            ("candidate export writes canonical unlabeled csv", CandidateExportWritesCanonicalUnlabeledCsv),
            ("train rejects candidate csv without labels", TrainRejectsCandidateCsvWithoutLabels),
            ("train rejects invalid label values", TrainRejectsInvalidLabelValues),
            ("train rejects single class labeled dataset", TrainRejectsSingleClassDataset),
            ("validator accepts valid labeled dataset", ValidatorAcceptsValidLabeledDataset),
            ("export-only runs skip the security gate unless fail-on is explicit", ExportOnlyRunsSkipGateByDefault),
            ("csproj input scans project files", CsprojInputScansProjectFiles),
            ("solution input scans all projects deterministically", SolutionInputScansProjectsDeterministically),
            ("project scanning excludes noise and generated files", ProjectScanningExcludesNoiseAndGeneratedFiles),
        };

        if (args.Length > 0)
        {
            tests = tests
                .Where(test => test.Name.Contains(args[0], StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        var failed = 0;

        foreach (var (name, test) in tests)
        {
            try
            {
                test();
                Console.WriteLine($"PASS {name}");
            }
            catch (Exception ex)
            {
                failed++;
                Console.WriteLine($"FAIL {name}");
                Console.WriteLine(ex);
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Tests run: {tests.Length}, Passed: {tests.Length - failed}, Failed: {failed}");

        return failed == 0 ? 0 : 1;
    }

    private static void CandidateExportWritesCanonicalUnlabeledCsv()
    {
        var tempDir = CreateTempDirectory();

        try
        {
            var outputPath = Path.Combine(tempDir, "training-candidates.csv");
            var exporter = new CsvTrainingExporter();
            exporter.Append(outputPath, new[] { CreateWeakHashingFinding() });

            var lines = File.ReadAllLines(outputPath);

            AssertEx.Equal(
                "RuleId,CweId,SnippetLength,HasJwtShape,HasBase64Shape,HasUrlShape,HasPlaceholderValue",
                lines[0]);
            AssertEx.Equal("VULN-WEAK-HASHING,CWE-327,12,0,0,0,0", lines[1]);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static void TrainRejectsCandidateCsvWithoutLabels()
    {
        var tempDir = CreateTempDirectory();

        try
        {
            var csvPath = Path.Combine(tempDir, "training-candidates.csv");
            var modelPath = Path.Combine(tempDir, "model.zip");

            File.WriteAllLines(csvPath, new[]
            {
                "RuleId,CweId,SnippetLength,HasJwtShape,HasBase64Shape,HasUrlShape,HasPlaceholderValue",
                "VULN-WEAK-HASHING,CWE-327,12,0,0,0,0"
            });

            AssertEx.Throws<InvalidOperationException>(
                () => TrainModel.Train(csvPath, modelPath),
                "labeled");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static void TrainRejectsInvalidLabelValues()
    {
        var tempDir = CreateTempDirectory();

        try
        {
            var csvPath = Path.Combine(tempDir, "training-labeled.csv");
            var modelPath = Path.Combine(tempDir, "model.zip");

            File.WriteAllLines(csvPath, new[]
            {
                "Label,RuleId,CweId,SnippetLength,HasJwtShape,HasBase64Shape,HasUrlShape,HasPlaceholderValue",
                "-1,VULN-WEAK-HASHING,CWE-327,12,0,0,0,0"
            });

            AssertEx.Throws<InvalidOperationException>(
                () => TrainModel.Train(csvPath, modelPath),
                "0 or 1");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static void TrainRejectsSingleClassDataset()
    {
        var tempDir = CreateTempDirectory();

        try
        {
            var csvPath = Path.Combine(tempDir, "training-labeled.csv");
            var modelPath = Path.Combine(tempDir, "model.zip");

            File.WriteAllLines(csvPath, new[]
            {
                "Label,RuleId,CweId,SnippetLength,HasJwtShape,HasBase64Shape,HasUrlShape,HasPlaceholderValue",
                "1,VULN-WEAK-HASHING,CWE-327,12,0,0,0,0",
                "1,VULN-WEAK-HASHING,CWE-327,10,0,0,0,0",
                "1,VULN-HARDCODED-SECRET,CWE-798,45,1,0,0,0"
            });

            AssertEx.Throws<InvalidOperationException>(
                () => TrainModel.Train(csvPath, modelPath),
                "both classes");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static void ValidatorAcceptsValidLabeledDataset()
    {
        var tempDir = CreateTempDirectory();

        try
        {
            var csvPath = Path.Combine(tempDir, "training-labeled.csv");

            File.WriteAllLines(csvPath, new[]
            {
                "Label,RuleId,CweId,SnippetLength,HasJwtShape,HasBase64Shape,HasUrlShape,HasPlaceholderValue",
                "1,VULN-WEAK-HASHING,CWE-327,12,0,0,0,0",
                "1,VULN-HARDCODED-SECRET,CWE-798,45,1,0,0,0",
                "0,VULN-HARDCODED-SECRET,CWE-798,20,0,0,1,0",
                "0,VULN-HARDCODED-SECRET,CWE-798,12,0,0,0,1"
            });

            var summary = TrainingDatasetValidator.ValidateLabeledDataset(csvPath);

            AssertEx.Equal(4, summary.RowCount);
            AssertEx.Equal(2, summary.PositiveCount);
            AssertEx.Equal(2, summary.NegativeCount);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static void ExportOnlyRunsSkipGateByDefault()
    {
        AssertEx.True(ExitCodePolicy.ShouldSkipGateForExport(exportTraining: true, failOnSpecified: false));
        AssertEx.True(!ExitCodePolicy.ShouldSkipGateForExport(exportTraining: true, failOnSpecified: true));
        AssertEx.True(!ExitCodePolicy.ShouldSkipGateForExport(exportTraining: false, failOnSpecified: false));

        var exitCode = ExitCodePolicy.GetExitCode(Severity.Critical, Severity.High, skipGate: true);
        AssertEx.Equal(0, exitCode);
    }

    private static void CsprojInputScansProjectFiles()
    {
        var tempDir = CreateTempDirectory();

        try
        {
            var projectDir = Path.Combine(tempDir, "ProjectOne");
            Directory.CreateDirectory(projectDir);

            var projectPath = Path.Combine(projectDir, "ProjectOne.csproj");
            File.WriteAllText(projectPath, CreateSdkStyleProject());

            var sourcePath = Path.Combine(projectDir, "Hashing.cs");
            File.WriteAllText(sourcePath, """
using System.Security.Cryptography;

public class Hashing
{
    public void Run()
    {
        var md5 = MD5.Create();
    }
}
""");

            var analyzer = new RoslynCodeAnalyzer();
            var findings = analyzer.AnalyzeDirectory(projectPath);

            AssertEx.Equal(1, findings.Count);
            AssertEx.Equal(sourcePath, findings.Single().FilePath);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static void SolutionInputScansProjectsDeterministically()
    {
        var tempDir = CreateTempDirectory();

        try
        {
            var projectADir = Path.Combine(tempDir, "AProject");
            var projectBDir = Path.Combine(tempDir, "BProject");
            Directory.CreateDirectory(projectADir);
            Directory.CreateDirectory(projectBDir);

            var projectAPath = Path.Combine(projectADir, "AProject.csproj");
            var projectBPath = Path.Combine(projectBDir, "BProject.csproj");

            File.WriteAllText(projectAPath, CreateSdkStyleProject());
            File.WriteAllText(projectBPath, CreateSdkStyleProject());

            var sourceAPath = Path.Combine(projectADir, "ASecrets.cs");
            var sourceBPath = Path.Combine(projectBDir, "BSecrets.cs");

            File.WriteAllText(sourceAPath, CreateSecretSource("Token"));
            File.WriteAllText(sourceBPath, CreateSecretSource("ApiToken"));

            var solutionPath = Path.Combine(tempDir, "Workspace.sln");
            File.WriteAllText(solutionPath, CreateSolutionFile(("BProject", Path.Combine("BProject", "BProject.csproj")),
                ("AProject", Path.Combine("AProject", "AProject.csproj"))));

            var analyzer = new RoslynCodeAnalyzer();
            var findings = analyzer.AnalyzeDirectory(solutionPath).ToList();

            AssertEx.Equal(2, findings.Count);
            AssertEx.Equal(sourceAPath, findings[0].FilePath);
            AssertEx.Equal(sourceBPath, findings[1].FilePath);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static void ProjectScanningExcludesNoiseAndGeneratedFiles()
    {
        var tempDir = CreateTempDirectory();

        try
        {
            var projectDir = Path.Combine(tempDir, "ProjectOne");
            Directory.CreateDirectory(projectDir);
            Directory.CreateDirectory(Path.Combine(projectDir, "bin"));
            Directory.CreateDirectory(Path.Combine(projectDir, "obj"));
            Directory.CreateDirectory(Path.Combine(projectDir, ".git"));

            var projectPath = Path.Combine(projectDir, "ProjectOne.csproj");
            File.WriteAllText(projectPath, CreateSdkStyleProject());

            var realSourcePath = Path.Combine(projectDir, "RealSecrets.cs");
            File.WriteAllText(realSourcePath, CreateSecretSource("Token"));

            File.WriteAllText(Path.Combine(projectDir, "bin", "BinSecrets.cs"), CreateSecretSource("BinToken"));
            File.WriteAllText(Path.Combine(projectDir, "obj", "ObjSecrets.cs"), CreateSecretSource("ObjToken"));
            File.WriteAllText(Path.Combine(projectDir, ".git", "GitSecrets.cs"), CreateSecretSource("GitToken"));
            File.WriteAllText(Path.Combine(projectDir, "Generated.g.cs"), CreateSecretSource("GeneratedToken"));
            File.WriteAllText(Path.Combine(projectDir, "Form.Designer.cs"), CreateSecretSource("DesignerToken"));

            var analyzer = new RoslynCodeAnalyzer();
            var findings = analyzer.AnalyzeDirectory(projectPath).ToList();

            AssertEx.Equal(1, findings.Count);
            AssertEx.Equal(realSourcePath, findings[0].FilePath);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static Finding CreateWeakHashingFinding() =>
        new()
        {
            Vulnerability = new Vulnerability
            {
                Id = "VULN-WEAK-HASHING",
                Name = "Use of weak cryptographic hashing algorithm",
                Description = "Uses MD5 or SHA1.",
                CWEId = "CWE-327",
                Severity = Severity.High,
                Recommandation = "Use SHA-256 or stronger."
            },
            FilePath = "Sample.cs",
            Line = 1,
            Column = 1,
            CodeSnippet = "MD5.Create()"
        };

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "AnalyzerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string CreateSdkStyleProject() =>
        """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
""";

    private static string CreateSecretSource(string propertyName) =>
        $$"""
public class Secrets
{
    public string {{propertyName}} { get; } = "JhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.abc.123";
}
""";

    private static string CreateSolutionFile(params (string Name, string RelativeProjectPath)[] projects)
    {
        var lines = new List<string>
        {
            "Microsoft Visual Studio Solution File, Format Version 12.00",
            "# Visual Studio Version 17"
        };

        foreach (var (name, relativeProjectPath) in projects)
        {
            lines.Add($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{name}\", \"{relativeProjectPath}\", \"{{{Guid.NewGuid().ToString().ToUpperInvariant()}}}\"");
            lines.Add("EndProject");
        }

        lines.Add("Global");
        lines.Add("EndGlobal");

        return string.Join(Environment.NewLine, lines);
    }
}
