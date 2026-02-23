using CountTokens;
using CountTokens_Tester.Aivax;
using System.Text.Json;

namespace CountTokens_Tester.Accuracy;

internal sealed class Runner
{
    private readonly AivaxClient? _client;
    private readonly PromptEstimator _estimator;
    private readonly string _model;
    private readonly int _maxConcurrency;
    private readonly Action<RunnerProgress>? _progress;

    public Runner(AivaxClient? client, PromptEstimator estimator, string model, int maxConcurrency = 1, Action<RunnerProgress>? progress = null)
    {
        _client = client;
        _estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));
        _model = string.IsNullOrWhiteSpace(model) ? throw new ArgumentException("Model is required", nameof(model)) : model;
        _maxConcurrency = Math.Max(1, maxConcurrency);
        _progress = progress;
    }

    public async Task<IReadOnlyList<AccuracyRow>> RunAsync(IReadOnlyList<AccuracyTestCase> cases, bool live, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cases);

        var rows = new AccuracyRow[cases.Count];
        using var gate = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);

        async Task RunOneAsync(int index, AccuracyTestCase test)
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                SafeReport(new RunnerProgress(_model, index + 1, cases.Count, test.Name, RunnerProgressStage.Started, null));
                AccuracyRow row = await RunCaseAsync(test, live, cancellationToken);
                rows[index] = row;
                SafeReport(new RunnerProgress(_model, index + 1, cases.Count, test.Name, RunnerProgressStage.Completed, row));
            }
            finally
            {
                gate.Release();
            }
        }

        var tasks = new Task[cases.Count];
        for (int i = 0; i < cases.Count; i++)
        {
            int idx = i;
            AccuracyTestCase test = cases[i];
            tasks[i] = RunOneAsync(idx, test);
        }

        await Task.WhenAll(tasks);
        return rows;
    }

    private void SafeReport(RunnerProgress progress)
    {
        if (_progress is null)
            return;

        try { _progress(progress); }
        catch { }
    }

    private async Task<AccuracyRow> RunCaseAsync(AccuracyTestCase test, bool live, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int estimated;
        try
        {
            estimated = await _estimator.EstimateAsync(test.Messages, cancellationToken);
        }
        catch (NotSupportedException ex)
        {
            return new AccuracyRow(test.Name, null, 0, "Unsupported modality: " + ex.Message, null);
        }
        catch (Exception ex)
        {
            return new AccuracyRow(test.Name, null, 0, null, "Estimator error: " + ex.Message);
        }

        if (!live)
            return new AccuracyRow(test.Name, null, estimated, "Live mode disabled (no LLM call)", null);

        if (_client is null)
            return new AccuracyRow(test.Name, null, estimated, null, "Runner misconfigured: live=true but client is null");

        try
        {
            var request = new ChatCompletionRequest(
                Model: _model,
                Messages: test.Messages,
                Stream: false,
                Temperature: 0,
                MaxTokens: 32
            );

            if (test.MaxRequestBytes is { } maxBytes)
            {
                byte[] json = JsonSerializer.SerializeToUtf8Bytes(request, JsonDefaults.SerializerOptions);
                if (json.Length > maxBytes)
                    return new AccuracyRow(test.Name, null, estimated, $"Request too large ({json.Length} bytes > {maxBytes})", null);
            }

            ChatCompletionResponse resp = await _client.CreateChatCompletionAsync(request, cancellationToken);

            int? actual = resp.Usage?.PromptTokens ?? resp.Usage?.InputTokens;
            if (actual is null)
                return new AccuracyRow(test.Name, null, estimated, "Response missing usage.prompt_tokens/input_tokens", null);

            return new AccuracyRow(test.Name, actual, estimated, null, null);
        }
        catch (AivaxApiException api)
        {
            return new AccuracyRow(test.Name, null, estimated, null, $"API error HTTP {api.StatusCode}: {Trim(api.ResponseBody, 500)}");
        }
        catch (Exception ex)
        {
            return new AccuracyRow(test.Name, null, estimated, null, "Unhandled error: " + ex.Message);
        }
    }

    private static string Trim(string s, int max)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= max)
            return s;

        return s[..max] + "…";
    }
}

internal enum RunnerProgressStage
{
    Started = 1,
    Completed = 2,
}

internal readonly record struct RunnerProgress(
    string Model,
    int CaseIndex,
    int TotalCases,
    string CaseName,
    RunnerProgressStage Stage,
    AccuracyRow? Row
);
