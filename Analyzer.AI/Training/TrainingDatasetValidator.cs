using System.Globalization;
using System.Text;
using Analyzer.Core.Training;

namespace Analyzer.AI.Training;

public static class TrainingDatasetValidator
{
    public static TrainingDatasetSummary ValidateLabeledDataset(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException("Training CSV not found.", csvPath);
        }

        var lines = File.ReadAllLines(csvPath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        if (lines.Length < 2)
        {
            throw new InvalidOperationException(
                $"Training CSV must contain the labeled header '{TrainingCsvSchema.LabeledHeader}' and at least one data row.");
        }

        var header = lines[0].Trim();
        if (string.Equals(header, TrainingCsvSchema.CandidateHeader, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected a labeled dataset for training. Use '{TrainingCsvSchema.LabeledFileName}' for training and '{TrainingCsvSchema.CandidateFileName}' only for export or review.");
        }

        if (!string.Equals(header, TrainingCsvSchema.LabeledHeader, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Invalid training CSV header. Expected '{TrainingCsvSchema.LabeledHeader}'.");
        }

        var positives = 0;
        var negatives = 0;

        for (var i = 1; i < lines.Length; i++)
        {
            var rowNumber = i + 1;
            var columns = ParseCsvLine(lines[i]);

            if (columns.Count != TrainingCsvSchema.LabeledColumns.Length)
            {
                throw new InvalidOperationException(
                    $"Row {rowNumber} has {columns.Count} columns. Expected {TrainingCsvSchema.LabeledColumns.Length}.");
            }

            var label = columns[0].Trim();
            if (label is not "0" and not "1")
            {
                throw new InvalidOperationException(
                    $"Invalid label '{label}' at row {rowNumber}. Labels must be 0 or 1.");
            }

            if (string.IsNullOrWhiteSpace(columns[1]))
            {
                throw new InvalidOperationException($"Row {rowNumber} is missing a rule id.");
            }

            if (string.IsNullOrWhiteSpace(columns[2]))
            {
                throw new InvalidOperationException($"Row {rowNumber} is missing a CWE id.");
            }

            ValidateInteger(columns[3], rowNumber, TrainingCsvSchema.SnippetLengthColumn);
            ValidateBinaryFeature(columns[4], rowNumber, TrainingCsvSchema.HasJwtShapeColumn);
            ValidateBinaryFeature(columns[5], rowNumber, TrainingCsvSchema.HasBase64ShapeColumn);
            ValidateBinaryFeature(columns[6], rowNumber, TrainingCsvSchema.HasUrlShapeColumn);
            ValidateBinaryFeature(columns[7], rowNumber, TrainingCsvSchema.HasPlaceholderValueColumn);

            if (label == "1")
            {
                positives++;
            }
            else
            {
                negatives++;
            }
        }

        if (positives == 0 || negatives == 0)
        {
            throw new InvalidOperationException(
                "Training dataset must contain both classes (0 and 1) to train a binary classifier.");
        }

        return new TrainingDatasetSummary(lines.Length - 1, positives, negatives);
    }

    private static void ValidateInteger(string value, int rowNumber, string columnName)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            throw new InvalidOperationException(
                $"Invalid integer value '{value}' in column '{columnName}' at row {rowNumber}.");
        }
    }

    private static void ValidateBinaryFeature(string value, int rowNumber, string columnName)
    {
        if (value is not "0" and not "1")
        {
            throw new InvalidOperationException(
                $"Invalid value '{value}' in column '{columnName}' at row {rowNumber}. Expected 0 or 1.");
        }
    }

    private static List<string> ParseCsvLine(string line)
    {
        var columns = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (c == ',' && !inQuotes)
            {
                columns.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        columns.Add(current.ToString());
        return columns;
    }
}
