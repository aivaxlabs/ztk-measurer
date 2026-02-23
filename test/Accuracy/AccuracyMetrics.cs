namespace CountTokens_Tester.Accuracy;

internal sealed record AccuracyRow(
    string Name,
    int? ActualPromptTokens,
    int EstimatedPromptTokens,
    string? SkippedReason,
    string? Error
)
{
    public int? Diff => ActualPromptTokens is null ? null : EstimatedPromptTokens - ActualPromptTokens.Value;

    public double? RelativeError
        => ActualPromptTokens is { } actual && actual > 0
            ? Math.Abs(EstimatedPromptTokens - actual) / (double)actual
            : null;

    public double? Precision
        => RelativeError is { } re
            ? Math.Clamp(1d - re, 0d, 1d)
            : null;
}

internal sealed record AccuracySummary(
    int Ran,
    int Skipped,
    int Failed,
    double? WeightedPrecision,
    double? MeanPrecision
);

internal static class AccuracyMath
{
    public static AccuracySummary Summarize(IReadOnlyList<AccuracyRow> rows)
    {
        int ran = 0;
        int skipped = 0;
        int failed = 0;

        long sumActual = 0;
        long sumAbsDiff = 0;

        double meanPrecisionSum = 0;
        int meanPrecisionCount = 0;

        foreach (AccuracyRow row in rows)
        {
            if (row.Error is not null)
            {
                failed++;
                continue;
            }

            if (row.SkippedReason is not null)
            {
                skipped++;
                continue;
            }

            ran++;

            if (row.ActualPromptTokens is { } actual && actual > 0)
            {
                sumActual += actual;
                sumAbsDiff += Math.Abs(row.EstimatedPromptTokens - actual);
            }

            if (row.Precision is { } precision)
            {
                meanPrecisionSum += precision;
                meanPrecisionCount++;
            }
        }

        double? weightedPrecision = sumActual > 0 ? Math.Clamp(1d - (sumAbsDiff / (double)sumActual), 0d, 1d) : null;
        double? meanPrecision = meanPrecisionCount > 0 ? meanPrecisionSum / meanPrecisionCount : null;

        return new AccuracySummary(ran, skipped, failed, weightedPrecision, meanPrecision);
    }
}
