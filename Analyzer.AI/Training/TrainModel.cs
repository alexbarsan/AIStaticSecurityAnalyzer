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

        var model = pipeline.Fit(data);

        ml.Model.Save(model, data.Schema, modelPath);
    }
}
