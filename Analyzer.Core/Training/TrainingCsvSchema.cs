namespace Analyzer.Core.Training;

public static class TrainingCsvSchema
{
    public const string LabelColumn = "Label";
    public const string RuleIdColumn = "RuleId";
    public const string CweIdColumn = "CweId";
    public const string SnippetLengthColumn = "SnippetLength";
    public const string HasJwtShapeColumn = "HasJwtShape";
    public const string HasBase64ShapeColumn = "HasBase64Shape";
    public const string HasUrlShapeColumn = "HasUrlShape";
    public const string HasPlaceholderValueColumn = "HasPlaceholderValue";

    public const string LabeledFileName = "training-labeled.csv";
    public const string CandidateFileName = "training-candidates.csv";

    public static readonly string[] CandidateColumns =
    [
        RuleIdColumn,
        CweIdColumn,
        SnippetLengthColumn,
        HasJwtShapeColumn,
        HasBase64ShapeColumn,
        HasUrlShapeColumn,
        HasPlaceholderValueColumn
    ];

    public static readonly string[] LabeledColumns =
    [
        LabelColumn,
        .. CandidateColumns
    ];

    public static string CandidateHeader => string.Join(",", CandidateColumns);
    public static string LabeledHeader => string.Join(",", LabeledColumns);
}
