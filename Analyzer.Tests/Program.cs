using Analyzer.AI.Training;
using Analyzer.Core.Models;
using Analyzer.Reporting;

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
}
