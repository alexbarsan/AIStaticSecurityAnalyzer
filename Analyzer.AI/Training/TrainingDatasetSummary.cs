namespace Analyzer.AI.Training;

public readonly record struct TrainingDatasetSummary(
    int RowCount,
    int PositiveCount,
    int NegativeCount);
