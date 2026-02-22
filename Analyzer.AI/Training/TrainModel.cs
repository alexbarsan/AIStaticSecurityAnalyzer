using Analyzer.AI.Models;
using Microsoft.ML;

namespace Analyzer.AI.Training;

public static class TrainModel
{
    public static void Train(string csvPath, string modelPath)
    {
        var ml = new MLContext(seed: 1);

        var data = ml.Data.LoadFromTextFile<AiInput>(
            path: csvPath,
            hasHeader: true,
            separatorChar: ',');

        // For tiny datasets, use a larger test fraction so we likely get both classes.
        var split = ml.Data.TrainTestSplit(data, testFraction: 0.4, seed: 1);

        var pipeline =
            ml.Transforms.Categorical.OneHotEncoding(nameof(AiInput.RuleId))
            .Append(ml.Transforms.Categorical.OneHotEncoding(nameof(AiInput.CweId)))
            .Append(ml.Transforms.Concatenate("Features",
                nameof(AiInput.RuleId),
                nameof(AiInput.CweId),
                nameof(AiInput.SnippetLength),
                nameof(AiInput.HasJwtShape),
                nameof(AiInput.HasBase64Shape),
                nameof(AiInput.HasUrlShape),
                nameof(AiInput.HasPlaceholderValue)))
            .Append(ml.BinaryClassification.Trainers.SdcaLogisticRegression(
                labelColumnName: nameof(AiInput.Label),
                featureColumnName: "Features"));

        var model = pipeline.Fit(split.TrainSet);

        // Evaluate safely (AUC requires both classes in test set)
        var predictions = model.Transform(split.TestSet);
        var metrics = ml.BinaryClassification.Evaluate(
            predictions,
            labelColumnName: nameof(AiInput.Label));

        // Check class distribution in test set
        var testRows = ml.Data.CreateEnumerable<AiInput>(split.TestSet, reuseRowObject: false).ToList();
        var pos = testRows.Count(r => r.Label);
        var neg = testRows.Count - pos;

        Console.WriteLine("AI model evaluation:");
        Console.WriteLine($"  Test set size: {testRows.Count} (pos={pos}, neg={neg})");
        Console.WriteLine($"  Accuracy:      {metrics.Accuracy:0.###}");
        Console.WriteLine($"  F1:            {metrics.F1Score:0.###}");

        if (pos > 0 && neg > 0)
            Console.WriteLine($"  AUC:           {metrics.AreaUnderRocCurve:0.###}");
        else
            Console.WriteLine("  AUC:           N/A (test set has a single class)");

        ml.Model.Save(model, data.Schema, modelPath);
    }
}
