using Analyzer.AI.Models;
using Analyzer.Core.Models;
using Microsoft.ML;

namespace Analyzer.AI;

public sealed class AiScorer
{
    private readonly MLContext _ml = new(seed: 1);
    private ITransformer? _model;

    // For MVP we load a model if it exists; otherwise we fall back to "no change"
    public void LoadModel(string modelPath)
    {
        if (!File.Exists(modelPath))
            return;

        _model = _ml.Model.Load(modelPath, out _);
    }

    public void ScoreFindings(IReadOnlyCollection<Finding> findings)
    {
        if (_model is null) return;

        var engine = _ml.Model.CreatePredictionEngine<AiInput, AiOutput>(_model);

        foreach (var f in findings)
        {
            var input = ToAiInput(f);
            var output = engine.Predict(input);

            // Probability becomes confidence; clamp to [0..1]
            f.Confidence = Math.Clamp(output.Probability, 0.0, 1.0);
        }
    }

    private static AiInput ToAiInput(Finding f)
    {
        var snippet = f.CodeSnippet ?? string.Empty;
        var lower = snippet.ToLowerInvariant();

        // Note: we don't parse strings here; we keep it simple and explainable
        bool jwt = snippet.Count(c => c == '.') >= 2 && snippet.Length >= 20;
        bool url = lower.Contains("http://") || lower.Contains("https://") || lower.Contains("localhost");
        bool placeholder = lower.Contains("changeme") || lower.Contains("your_api_key") || lower.Contains("placeholder") || lower.Contains("\"test\"");

        // base64-ish: letters/digits/+/_/-/= and reasonably long
        bool base64ish = snippet.Length >= 20 &&
                         snippet.All(c => char.IsLetterOrDigit(c) || c is '+' or '/' or '=' or '-' or '_');

        return new AiInput
        {
            // Label not used during inference
            Label = true,

            RuleId = f.Vulnerability.Id ?? string.Empty,
            CweId = f.Vulnerability.CWEId ?? string.Empty,
            SnippetLength = snippet.Length,
            HasJwtShape = jwt ? 1f : 0f,
            HasBase64Shape = base64ish ? 1f : 0f,
            HasUrlShape = url ? 1f : 0f,
            HasPlaceholderValue = placeholder ? 1f : 0f
        };
    }
}
