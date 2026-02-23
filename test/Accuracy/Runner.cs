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

            ChatCompletionResponse resp = await CreateWithRetryAsync(request, cancellationToken);

            int? actual = ComputeActualInputTokens(resp.Usage);
            if (actual is null)
                return new AccuracyRow(test.Name, null, estimated, "Response missing usage input token fields", null);

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AIVAX_DEBUG_USAGE")))
            {
                Usage? u = resp.Usage;
                Console.WriteLine(
                    $"[usage] case='{test.Name}' model='{_model}' " +
                    $"prompt={u?.PromptTokens} input={u?.InputTokens} cached_input={u?.CachedInputTokens} " +
                    $"audio={u?.AudioTokens} cached_audio={u?.CachedAudioInputTokens} " +
                    $"details.cached={u?.PromptTokensDetails?.CachedTokens} details.audio={u?.PromptTokensDetails?.AudioTokens} " +
                    $"details.cached_audio={u?.PromptTokensDetails?.CachedAudioTokens ?? u?.PromptTokensDetails?.CachedAudioInputTokens} => actual={actual}"
                );
            }

            return new AccuracyRow(test.Name, actual, estimated, null, null);
        }
        catch (AivaxApiException api)
        {
            return new AccuracyRow(test.Name, null, estimated, null, $"API error HTTP {api.StatusCode}: {Trim(api.ResponseBody, 500)}");
        }
        catch (TaskCanceledException)
        {
            return new AccuracyRow(test.Name, null, estimated, null, "Request timed out");
        }
        catch (Exception ex)
        {
            return new AccuracyRow(test.Name, null, estimated, null, "Unhandled error: " + ex.Message);
        }
    }

    private async Task<ChatCompletionResponse> CreateWithRetryAsync(ChatCompletionRequest request, CancellationToken cancellationToken)
    {
        if (_client is null)
            throw new InvalidOperationException("Runner misconfigured: client is null");

        const int maxAttempts = 3;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await _client.CreateChatCompletionAsync(request, cancellationToken);
            }
            catch (AivaxApiException ex) when (attempt < maxAttempts && IsTransientStatus(ex.StatusCode))
            {
                int delayMs = attempt switch
                {
                    1 => 500,
                    2 => 1500,
                    _ => 2500,
                };

                await Task.Delay(delayMs, cancellationToken);
            }
            catch (TaskCanceledException) when (attempt < maxAttempts)
            {
                int delayMs = attempt switch
                {
                    1 => 500,
                    2 => 1500,
                    _ => 2500,
                };

                await Task.Delay(delayMs, cancellationToken);
            }
        }

        // Unreachable: loop either returns or throws on the last attempt.
        throw new InvalidOperationException("Retry loop exhausted unexpectedly.");
    }

    private static bool IsTransientStatus(int statusCode)
        => statusCode is 408 or 429 or 500 or 502 or 503 or 504 or 520 or 521 or 522 or 523 or 524;

    private static string Trim(string s, int max)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= max)
            return s;

        return s[..max] + "…";
    }

    private static int? ComputeActualInputTokens(Usage? usage)
    {
        if (usage is null)
            return null;

        // Desired definition for the suite:
        // input_tokens + cached_input_tokens + audio_tokens + cached_audio_input_tokens
        // IMPORTANT: Some providers expose cached/audio as a BREAKDOWN of prompt_tokens
        // (e.g. prompt_tokens_details.cached_tokens). Those are NOT additive.

        bool hasAdditiveFields = usage.InputTokens is not null
            || usage.CachedInputTokens is not null
            || usage.AudioTokens is not null
            || usage.CachedAudioInputTokens is not null;

        if (hasAdditiveFields)
        {
            int input = usage.InputTokens ?? 0;
            int cachedInput = usage.CachedInputTokens ?? 0;
            int audio = usage.AudioTokens ?? 0;
            int cachedAudio = usage.CachedAudioInputTokens ?? 0;

            int total = input + cachedInput + audio + cachedAudio;
            return total > 0 ? total : null;
        }

        // Fallback: prompt_tokens is already the total input tokens (including cached/audio if any).
        return usage.PromptTokens ?? usage.InputTokens;
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
