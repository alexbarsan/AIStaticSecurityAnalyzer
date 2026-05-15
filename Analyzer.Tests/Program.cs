using Analyzer.AI.Training;
using Analyzer.Core.Execution;
using Analyzer.Core.Models;
using Analyzer.Core.Pipeline;
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
            ("weak hashing fixture positive sample is detected", WeakHashingFixturePositiveSampleIsDetected),
            ("weak hashing fixture negative sample is not detected", WeakHashingFixtureNegativeSampleIsNotDetected),
            ("hardcoded secret fixture positive sample is detected", HardcodedSecretFixturePositiveSampleIsDetected),
            ("hardcoded secret fixture negative sample is not detected", HardcodedSecretFixtureNegativeSampleIsNotDetected),
            ("csproj input scans project files", CsprojInputScansProjectFiles),
            ("solution input scans all projects deterministically", SolutionInputScansProjectsDeterministically),
            ("project scanning excludes noise and generated files", ProjectScanningExcludesNoiseAndGeneratedFiles),
            ("csproj respects Compile Remove items", CsprojRespectsCompileRemoveItems),
            ("directory build props can disable default compile items", DirectoryBuildPropsCanDisableDefaultCompileItems),
            ("csproj includes referenced project sources", CsprojIncludesReferencedProjectSources),
            ("csproj respects compile settings inside referenced projects", CsprojRespectsReferencedProjectCompileSettings),
            ("conditioned project references respect evaluated msbuild properties", ConditionedProjectReferencesRespectEvaluatedMsbuildProperties),
            ("project reference cycles do not loop or duplicate findings", ProjectReferenceCyclesDoNotLoopOrDuplicateFindings),
            ("final pipeline applies scoring before filtering and rendering", FinalPipelineAppliesScoringBeforeFilteringAndRendering),
            ("invalid analysis path returns a friendly console message", InvalidAnalysisPathReturnsFriendlyMessage),
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

    private static void WeakHashingFixturePositiveSampleIsDetected()
    {
        var findings = AnalyzeFixture("Rules/WeakHashing/Positive.cs").ToList();

        AssertEx.Equal(1, findings.Count);
        AssertEx.Equal("VULN-WEAK-HASHING", findings[0].Vulnerability.Id);
        AssertEx.Equal("CWE-327", findings[0].Vulnerability.CWEId);
        AssertEx.Equal(Severity.High, findings[0].Vulnerability.Severity);
        AssertEx.Equal("MD5.Create()", findings[0].CodeSnippet);
    }

    private static void WeakHashingFixtureNegativeSampleIsNotDetected()
    {
        var findings = AnalyzeFixture("Rules/WeakHashing/Negative.cs").ToList();

        AssertEx.Equal(0, findings.Count);
    }

    private static void HardcodedSecretFixturePositiveSampleIsDetected()
    {
        var findings = AnalyzeFixture("Rules/HardcodedSecret/Positive.cs").ToList();

        AssertEx.Equal(1, findings.Count);
        AssertEx.Equal("VULN-HARDCODED-SECRET", findings[0].Vulnerability.Id);
        AssertEx.Equal("CWE-798", findings[0].Vulnerability.CWEId);
        AssertEx.Equal(Severity.Critical, findings[0].Vulnerability.Severity);
        AssertEx.Contains("Token", findings[0].CodeSnippet);
    }

    private static void HardcodedSecretFixtureNegativeSampleIsNotDetected()
    {
        var findings = AnalyzeFixture("Rules/HardcodedSecret/Negative.cs").ToList();

        AssertEx.Equal(0, findings.Count);
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

    private static void CsprojRespectsCompileRemoveItems()
    {
        var tempDir = CreateTempDirectory();

        try
        {
            var projectDir = Path.Combine(tempDir, "ProjectOne");
            Directory.CreateDirectory(projectDir);

            var projectPath = Path.Combine(projectDir, "ProjectOne.csproj");
            File.WriteAllText(projectPath, CreateSdkStyleProjectWithCompileRemove("IgnoredSecrets.cs"));

            var keptSourcePath = Path.Combine(projectDir, "KeptSecrets.cs");
            var removedSourcePath = Path.Combine(projectDir, "IgnoredSecrets.cs");

            File.WriteAllText(keptSourcePath, CreateSecretSource("KeptToken"));
            File.WriteAllText(removedSourcePath, CreateSecretSource("IgnoredToken"));

            var analyzer = new RoslynCodeAnalyzer();
            var findings = analyzer.AnalyzeDirectory(projectPath).ToList();

            AssertEx.Equal(1, findings.Count);
            AssertEx.Equal(keptSourcePath, findings[0].FilePath);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static void CsprojExplicitCompileIncludeWorksWithDefaultItemsDisabled()
    {
        var tempDir = CreateTempDirectory();

        try
        {
            var projectDir = Path.Combine(tempDir, "ProjectOne");
            Directory.CreateDirectory(projectDir);

            var projectPath = Path.Combine(projectDir, "ProjectOne.csproj");
            File.WriteAllText(projectPath, CreateSdkStyleProjectWithCompileIncludeOnly("OnlySecrets.cs"));

            var includedSourcePath = Path.Combine(projectDir, "OnlySecrets.cs");
            var ignoredSourcePath = Path.Combine(projectDir, "IgnoredSecrets.cs");

            File.WriteAllText(includedSourcePath, CreateSecretSource("OnlyToken"));
            File.WriteAllText(ignoredSourcePath, CreateSecretSource("IgnoredToken"));

            var compileItems = GetMsbuildItemFullPaths(projectPath, "Compile").ToList();

            AssertEx.True(compileItems.Contains(includedSourcePath, StringComparer.OrdinalIgnoreCase));
            AssertEx.True(!compileItems.Contains(ignoredSourcePath, StringComparer.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static void DirectoryBuildPropsCanDisableDefaultCompileItems()
    {
        var tempDir = CreateTempDirectory();

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "Directory.Build.props"), CreateDirectoryBuildPropsWithDefaultCompileItemsDisabled());

            var projectDir = Path.Combine(tempDir, "ProjectOne");
            Directory.CreateDirectory(projectDir);

            var projectPath = Path.Combine(projectDir, "ProjectOne.csproj");
            File.WriteAllText(projectPath, CreateSdkStyleProjectWithExplicitCompileInclude("OnlySecrets.cs"));

            var includedSourcePath = Path.Combine(projectDir, "OnlySecrets.cs");
            var ignoredSourcePath = Path.Combine(projectDir, "IgnoredSecrets.cs");

            File.WriteAllText(includedSourcePath, CreateSecretSource("OnlyToken"));
            File.WriteAllText(ignoredSourcePath, CreateSecretSource("IgnoredToken"));

            var analyzer = new RoslynCodeAnalyzer();
            var findings = analyzer.AnalyzeDirectory(projectPath).ToList();

            AssertEx.Equal(1, findings.Count);
            AssertEx.Equal(includedSourcePath, findings[0].FilePath);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static void CsprojIncludesReferencedProjectSources()
    {
        var tempDir = CreateTempDirectory();

        try
        {
            var appDir = Path.Combine(tempDir, "App");
            var libDir = Path.Combine(tempDir, "Lib");
            Directory.CreateDirectory(appDir);
            Directory.CreateDirectory(libDir);

            var libProjectPath = Path.Combine(libDir, "Lib.csproj");
            File.WriteAllText(libProjectPath, CreateSdkStyleProject());
            File.WriteAllText(Path.Combine(libDir, "LibSecrets.cs"), CreateSecretSource("LibToken"));

            var appProjectPath = Path.Combine(appDir, "App.csproj");
            File.WriteAllText(appProjectPath, CreateSdkStyleProjectWithProjectReference(Path.Combine("..", "Lib", "Lib.csproj")));
            File.WriteAllText(Path.Combine(appDir, "AppSecrets.cs"), CreateSecretSource("AppToken"));

            var analyzer = new RoslynCodeAnalyzer();
            var findings = analyzer.AnalyzeDirectory(appProjectPath).ToList();

            AssertEx.Equal(2, findings.Count);
            AssertEx.Contains("AppSecrets.cs", findings[0].FilePath);
            AssertEx.Contains("LibSecrets.cs", findings[1].FilePath);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static void CsprojRespectsReferencedProjectCompileSettings()
    {
        var tempDir = CreateTempDirectory();

        try
        {
            var appDir = Path.Combine(tempDir, "App");
            var libDir = Path.Combine(tempDir, "Lib");
            Directory.CreateDirectory(appDir);
            Directory.CreateDirectory(libDir);

            var libProjectPath = Path.Combine(libDir, "Lib.csproj");
            File.WriteAllText(libProjectPath, CreateSdkStyleProjectWithCompileRemove("IgnoredLibSecrets.cs"));
            File.WriteAllText(Path.Combine(libDir, "KeptLibSecrets.cs"), CreateSecretSource("KeptLibToken"));
            File.WriteAllText(Path.Combine(libDir, "IgnoredLibSecrets.cs"), CreateSecretSource("IgnoredLibToken"));

            var appProjectPath = Path.Combine(appDir, "App.csproj");
            File.WriteAllText(appProjectPath, CreateSdkStyleProjectWithProjectReference(Path.Combine("..", "Lib", "Lib.csproj")));
            File.WriteAllText(Path.Combine(appDir, "AppSecrets.cs"), CreateSecretSource("AppToken"));

            var analyzer = new RoslynCodeAnalyzer();
            var findings = analyzer.AnalyzeDirectory(appProjectPath).ToList();

            AssertEx.Equal(2, findings.Count);
            AssertEx.Contains("AppSecrets.cs", findings[0].FilePath);
            AssertEx.Contains("KeptLibSecrets.cs", findings[1].FilePath);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static void ConditionedProjectReferencesRespectEvaluatedMsbuildProperties()
    {
        var tempDir = CreateTempDirectory();

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "Directory.Build.props"), CreateDirectoryBuildPropsWithProperty("IncludeLib", "false"));

            var appDir = Path.Combine(tempDir, "App");
            var libDir = Path.Combine(tempDir, "Lib");
            Directory.CreateDirectory(appDir);
            Directory.CreateDirectory(libDir);

            var libProjectPath = Path.Combine(libDir, "Lib.csproj");
            File.WriteAllText(libProjectPath, CreateSdkStyleProject());
            File.WriteAllText(Path.Combine(libDir, "LibSecrets.cs"), CreateSecretSource("LibToken"));

            var appProjectPath = Path.Combine(appDir, "App.csproj");
            File.WriteAllText(appProjectPath, CreateSdkStyleProjectWithConditionedProjectReference(Path.Combine("..", "Lib", "Lib.csproj"), "'$(IncludeLib)' == 'true'"));
            File.WriteAllText(Path.Combine(appDir, "AppSecrets.cs"), CreateSecretSource("AppToken"));

            var analyzer = new RoslynCodeAnalyzer();
            var findings = analyzer.AnalyzeDirectory(appProjectPath).ToList();

            AssertEx.Equal(1, findings.Count);
            AssertEx.Contains("AppSecrets.cs", findings[0].FilePath);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static void ProjectReferenceCyclesDoNotLoopOrDuplicateFindings()
    {
        var tempDir = CreateTempDirectory();

        try
        {
            var projectADir = Path.Combine(tempDir, "ProjectA");
            var projectBDir = Path.Combine(tempDir, "ProjectB");
            Directory.CreateDirectory(projectADir);
            Directory.CreateDirectory(projectBDir);

            var projectAPath = Path.Combine(projectADir, "ProjectA.csproj");
            var projectBPath = Path.Combine(projectBDir, "ProjectB.csproj");

            File.WriteAllText(projectAPath, CreateSdkStyleProjectWithProjectReferences(Path.Combine("..", "ProjectB", "ProjectB.csproj")));
            File.WriteAllText(projectBPath, CreateSdkStyleProjectWithProjectReferences(Path.Combine("..", "ProjectA", "ProjectA.csproj")));

            File.WriteAllText(Path.Combine(projectADir, "ASecrets.cs"), CreateSecretSource("AToken"));
            File.WriteAllText(Path.Combine(projectBDir, "BSecrets.cs"), CreateSecretSource("BToken"));

            var analyzer = new RoslynCodeAnalyzer();
            var findings = analyzer.AnalyzeDirectory(projectAPath).ToList();

            AssertEx.Equal(2, findings.Count);
            AssertEx.Equal(2, findings.Select(finding => finding.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            AssertEx.Contains("ASecrets.cs", findings[0].FilePath);
            AssertEx.Contains("BSecrets.cs", findings[1].FilePath);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static void FinalPipelineAppliesScoringBeforeFilteringAndRendering()
    {
        var lowFinding = CreateSecretFinding("LowToken", confidence: 1.0);
        var highFinding = CreateSecretFinding("HighToken", confidence: 1.0);
        var findings = new[] { lowFinding, highFinding };

        var processed = ScanPipelineProcessor.Process(
            findings,
            useAi: true,
            minConfidence: 0.5,
            scoreFindings: items =>
            {
                items[0].Confidence = 0.2;
                items[1].Confidence = 0.8;
            });

        AssertEx.Equal(1, processed.FinalFindings.Count);
        AssertEx.Equal("HighToken", processed.FinalFindings[0].CodeSnippet);
        AssertEx.Equal(1, processed.ConsoleLines.Count);
        AssertEx.Contains("conf 0.80", processed.ConsoleLines[0]);
        AssertEx.Contains("HighToken.cs", processed.ConsoleLines[0]);
    }

    private static void InvalidAnalysisPathReturnsFriendlyMessage()
    {
        var missingPath = Path.Combine("C:\\", "missing", "project.csproj");
        var ex = new FileNotFoundException("Analysis path was not found.", missingPath);

        var message = ScanErrorFormatter.Format(ex);

        AssertEx.Contains("Analysis path was not found", message);
        AssertEx.Contains(missingPath, message);
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

    private static Finding CreateSecretFinding(string name, double confidence) =>
        new()
        {
            Vulnerability = new Vulnerability
            {
                Id = "VULN-HARDCODED-SECRET",
                Name = "Hardcoded secret in source code",
                Description = "Sensitive information is stored in source code.",
                CWEId = "CWE-798",
                Severity = Severity.Critical,
                Recommandation = "Store secrets securely."
            },
            FilePath = $"{name}.cs",
            Line = 10,
            Column = 5,
            CodeSnippet = name,
            Confidence = confidence
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

    private static string CreateSdkStyleProjectWithCompileRemove(string removedFile) =>
        $$"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <Compile Remove="{{removedFile}}" />
  </ItemGroup>
</Project>
""";

    private static string CreateSdkStyleProjectWithExplicitCompileInclude(string includedFile) =>
        $$"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="{{includedFile}}" />
  </ItemGroup>
</Project>
""";

    private static string CreateSdkStyleProjectWithCompileIncludeOnly(string includedFile) =>
        $$"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="{{includedFile}}" />
  </ItemGroup>
</Project>
""";

    private static string CreateSdkStyleProjectWithProjectReference(string relativeProjectReference) =>
        CreateSdkStyleProjectWithProjectReferences(relativeProjectReference);

    private static string CreateSdkStyleProjectWithProjectReferences(params string[] relativeProjectReferences) =>
        $$"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
{{string.Join(Environment.NewLine, relativeProjectReferences.Select(projectReference => $"    <ProjectReference Include=\"{projectReference}\" />"))}}
  </ItemGroup>
</Project>
""";

    private static string CreateSdkStyleProjectWithConditionedProjectReference(string relativeProjectReference, string condition) =>
        $$"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="{{relativeProjectReference}}" Condition="{{condition}}" />
  </ItemGroup>
</Project>
""";

    private static string CreateDirectoryBuildPropsWithDefaultCompileItemsDisabled() =>
        """
<Project>
  <PropertyGroup>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
</Project>
""";

    private static string CreateDirectoryBuildPropsWithProperty(string name, string value) =>
        $$"""
<Project>
  <PropertyGroup>
    <{{name}}>{{value}}</{{name}}>
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

    private static IReadOnlyCollection<Finding> AnalyzeFixture(string relativeFixturePath)
    {
        var fixturePath = GetFixturePath(relativeFixturePath);
        var tempDir = CreateTempDirectory();

        try
        {
            var tempFixturePath = Path.Combine(tempDir, Path.GetFileName(fixturePath));
            File.Copy(fixturePath, tempFixturePath, overwrite: true);

            var analyzer = new RoslynCodeAnalyzer();
            return analyzer.AnalyzeDirectory(tempFixturePath);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static string GetFixturePath(string relativeFixturePath)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Analyzer.Tests", "Fixtures"));
        var fixturePath = Path.Combine(root, relativeFixturePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(fixturePath))
        {
            throw new FileNotFoundException($"Fixture was not found: {relativeFixturePath}", fixturePath);
        }

        return fixturePath;
    }

    private static string RunCli(string analysisPath, out int exitCode)
    {
        var cliPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Analyzer.CLI",
            "bin",
            "Debug",
            "net9.0",
            "Analyzer.CLI.dll"));

        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."))
        };

        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add(cliPath);
        startInfo.ArgumentList.Add(analysisPath);

        using var process = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start CLI process.");

        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        exitCode = process.ExitCode;
        return string.Concat(standardOutput, standardError);
    }

    private static IReadOnlyCollection<string> GetMsbuildItemFullPaths(string projectPath, string itemName)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory
        };

        startInfo.ArgumentList.Add("msbuild");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add($"-getItem:{itemName}");
        startInfo.ArgumentList.Add("-nologo");

        using var process = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start dotnet msbuild from test.");

        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"dotnet msbuild failed in test: {standardError}");
        }

        using var jsonDocument = System.Text.Json.JsonDocument.Parse(standardOutput);
        if (!jsonDocument.RootElement.TryGetProperty("Items", out var itemsElement) ||
            !itemsElement.TryGetProperty(itemName, out var itemArray))
        {
            return Array.Empty<string>();
        }

        return itemArray
            .EnumerateArray()
            .Select(item => item.TryGetProperty("FullPath", out var fullPathElement) ? fullPathElement.GetString() : null)
            .Where(fullPath => !string.IsNullOrWhiteSpace(fullPath))
            .Select(fullPath => Path.GetFullPath(fullPath!))
            .ToList();
    }

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
