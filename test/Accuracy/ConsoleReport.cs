namespace CountTokens_Tester.Accuracy;

internal static class ConsoleReport
{
    public static void Print(IReadOnlyList<AccuracyRow> rows)
    {
        Console.WriteLine("\nAccuracy results:\n");
        Console.WriteLine("Name|ActualPromptTokens|EstimatedPromptTokens|Diff|Precision|Status");

        foreach (AccuracyRow row in rows)
        {
            string status = row.Error is not null
                ? "ERROR"
                : row.SkippedReason is not null
                    ? "SKIP"
                    : "OK";

            string actual = row.ActualPromptTokens?.ToString() ?? "";
            string diff = row.Diff?.ToString() ?? "";
            string precision = row.Precision is { } p ? (p * 100d).ToString("0.00") + "%" : "";

            Console.WriteLine($"{row.Name}|{actual}|{row.EstimatedPromptTokens}|{diff}|{precision}|{status}");

            if (row.Error is not null)
                Console.WriteLine($"  Error: {row.Error}");
            else if (row.SkippedReason is not null)
                Console.WriteLine($"  Skip: {row.SkippedReason}");
        }

        AccuracySummary summary = AccuracyMath.Summarize(rows);
        Console.WriteLine("\nSummary:");
        Console.WriteLine($"  Ran: {summary.Ran}");
        Console.WriteLine($"  Skipped: {summary.Skipped}");
        Console.WriteLine($"  Failed: {summary.Failed}");
        if (summary.WeightedPrecision is { } wp)
            Console.WriteLine($"  Weighted precision: {(wp * 100d):0.00}%");
        if (summary.MeanPrecision is { } mp)
            Console.WriteLine($"  Mean precision: {(mp * 100d):0.00}%");
        Console.WriteLine();
    }
}
