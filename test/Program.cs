using CountTokens;
using CountTokens_Tester.Accuracy;
using CountTokens_Tester.Aivax;
using System.Diagnostics;

static ITokenMeasurer CreateMeasurer(string model)
{
    if (string.IsNullOrWhiteSpace(model))
        return new DefaultTokenMeasurer();

    string m = model.Trim();

    return m switch
    {
        "@google/gemini-2.0-flash-lite" => new DefaultTokenMeasurer(),
        "@google/gemini-2.5-flash-lite" => new DefaultTokenMeasurer(),
        "@google/gemini-3-flash" => new GeminiV3TokenMeasurer(),

        "@anthropic/claude-4.5-haiku" => new ClaudeV4TokenMeasurer(),

        "@metaai/llama-3.1-8b" => new LlamaV3TokenMeasurer(),
        "@metaai/llama-4-scout-17b-16e" => new LlamaV4TokenMeasurer(),

        "@openai/gpt-5-nano" => new OpenAiV5TokenMeasurer(),
        "@openai/gpt-4.1-nano" => new OpenAiV4_1TokenMeasurer(),
        "@openai/gpt-oss-20b" => new OpenAiOssTokenMeasurer(),

        "@qwen/qwen3-32b" => new QwenV3TokenMeasurer(),

        _ => new DefaultTokenMeasurer(),
    };
}

static string? GetArg(string[] cliArgs, string name)
{
    for (int i = 0; i < cliArgs.Length; i++)
    {
        if (string.Equals(cliArgs[i], name, StringComparison.OrdinalIgnoreCase) && i + 1 < cliArgs.Length)
            return cliArgs[i + 1];
    }

    return null;
}

static bool HasFlag(string[] cliArgs, string name)
    => cliArgs.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));

static IReadOnlyList<string> GetAllModels()
    => new[]
    {
        "@google/gemini-2.5-flash-lite",
        "@google/gemini-3-flash",
        "@anthropic/claude-4.5-haiku",
        "@metaai/llama-3.1-8b",
        "@metaai/llama-4-scout-17b-16e",
        "@openai/gpt-5-nano",
        "@openai/gpt-4.1-nano",
        "@openai/gpt-oss-20b",
        "@qwen/qwen3-32b",
    };

static CaseKind GetCaseKind(string name)
{
    if (string.IsNullOrWhiteSpace(name))
        return CaseKind.Unknown;

    if (name.StartsWith("text-", StringComparison.OrdinalIgnoreCase))
        return CaseKind.Text;
    if (name.StartsWith("image-", StringComparison.OrdinalIgnoreCase))
        return CaseKind.Image;
    if (name.StartsWith("audio-", StringComparison.OrdinalIgnoreCase))
        return CaseKind.Audio;
    if (name.StartsWith("video-", StringComparison.OrdinalIgnoreCase))
        return CaseKind.Video;
    if (name.StartsWith("pdf-", StringComparison.OrdinalIgnoreCase))
        return CaseKind.Pdf;
    if (name.StartsWith("mixed-", StringComparison.OrdinalIgnoreCase))
        return CaseKind.Mixed;

    return CaseKind.Unknown;
}

static double? ComputeWeightedPrecision(IReadOnlyList<AccuracyRow> rows, Func<AccuracyRow, bool> include)
{
    long sumActual = 0;
    long sumAbsDiff = 0;

    foreach (AccuracyRow row in rows)
    {
        if (!include(row))
            continue;
        if (row.Error is not null)
            continue;
        if (row.SkippedReason is not null)
            continue;
        if (row.ActualPromptTokens is not { } actual || actual <= 0)
            continue;

        sumActual += actual;
        sumAbsDiff += Math.Abs(row.EstimatedPromptTokens - actual);
    }

    if (sumActual <= 0)
        return null;

    return Math.Clamp(1d - (sumAbsDiff / (double)sumActual), 0d, 1d);
}

static string FormatPercent(double? p)
    => p is { } v ? (v * 100d).ToString("0") + "%" : "";

static string FormatPercentMd(double? p)
    => p is { } ? FormatPercent(p) : "—";

static string EscapeMdCell(string s)
    => string.IsNullOrEmpty(s) ? "" : s.Replace("|", "\\|");

static string GoalEmoji(double? overall)
{
    if (overall is not { } v)
        return "⚪";

    return v > 0.95d ? "✅" : "❌";
}

static string Trunc(string s, int max)
{
    if (string.IsNullOrEmpty(s) || s.Length <= max)
        return s;
    return s[..max] + "…";
}

static void PrintUsage()
{
    Console.WriteLine("CountTokens_Tester - accuracy runner");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project CountTokens_Tester -- --live");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --live                 Calls the LLM endpoint and computes accuracy");
    Console.WriteLine("  --all-models           Runs the suite for all known models (overrides --model)");
    Console.WriteLine("  --model <name>          Overrides model (default: @google/gemini-2.5-flash-lite)");
    Console.WriteLine("  --endpoint <url>        Overrides endpoint (default: https://inference.aivax.net/v1/chat/completions)");
    Console.WriteLine("  --per-message-overhead <int>  Adds a fixed overhead per message (default: 0)");
    Console.WriteLine();
    Console.WriteLine("Environment:");
    Console.WriteLine("  AIVAX_API_KEY           Bearer token (required for --live)");
}

string[] cliArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
if (HasFlag(cliArgs, "--help") || HasFlag(cliArgs, "-h"))
{
    PrintUsage();
    return;
}

bool live = HasFlag(cliArgs, "--live");
bool allModels = HasFlag(cliArgs, "--all-models");

string model = GetArg(cliArgs, "--model") ?? "@google/gemini-2.5-flash-lite";
string endpointRaw = GetArg(cliArgs, "--endpoint") ?? "https://inference.aivax.net/v1/chat/completions";

int perMessageOverhead = 0;
if (int.TryParse(GetArg(cliArgs, "--per-message-overhead"), out int parsedOverhead) && parsedOverhead >= 0)
    perMessageOverhead = parsedOverhead;

string? apiKey = Environment.GetEnvironmentVariable("AIVAX_API_KEY");
if (live && string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("Missing env var AIVAX_API_KEY (required for --live).\n");
    PrintUsage();
    Environment.ExitCode = 2;
    return;
}

if (!Uri.TryCreate(endpointRaw, UriKind.Absolute, out Uri? endpoint))
{
    Console.WriteLine("Invalid --endpoint URL.\n");
    PrintUsage();
    Environment.ExitCode = 2;
    return;
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

string mediaDir = Path.Combine(AppContext.BaseDirectory, "Media");
if (!Directory.Exists(mediaDir))
{
    Console.WriteLine($"Media directory not found at '{mediaDir}'.");
    Environment.ExitCode = 2;
    return;
}

IReadOnlyList<AccuracyTestCase> cases = TestCases.Create(mediaDir);

using var http = new HttpClient
{
    Timeout = TimeSpan.FromMinutes(3)
};

AivaxClient? client = live ? new AivaxClient(http, endpoint, apiKey ?? string.Empty) : null;

int maxConcurrency = live ? 6 : Math.Max(1, Environment.ProcessorCount);

IReadOnlyList<string> modelsToRun = allModels ? GetAllModels() : new[] { model };

var allSummaries = allModels ? new List<ModelSummary>(modelsToRun.Count) : null;

int modelIndex = 0;
int modelTotal = modelsToRun.Count;

foreach (string modelToRun in modelsToRun)
{
    modelIndex++;

    ITokenMeasurer measurer = CreateMeasurer(modelToRun);
    var estimator = new PromptEstimator(measurer, perMessageOverhead);

    object progressLock = new();
    int completed = 0;
    int inFlight = 0;
    string current = string.Empty;
    long lastUpdateMs = 0;
    var sw = Stopwatch.StartNew();

    void Progress(RunnerProgress p)
    {
        lock (progressLock)
        {
            if (p.Stage == RunnerProgressStage.Started)
            {
                inFlight++;
                current = p.CaseName;
            }
            else if (p.Stage == RunnerProgressStage.Completed)
            {
                completed++;
                inFlight = Math.Max(0, inFlight - 1);
                current = p.CaseName;
            }

            long now = sw.ElapsedMilliseconds;
            bool shouldUpdate = p.Stage == RunnerProgressStage.Completed || (now - lastUpdateMs) >= 200;
            if (!shouldUpdate)
                return;

            lastUpdateMs = now;

            string line = $"[{modelIndex}/{modelTotal}] {Trunc(modelToRun, 42)} | {completed}/{p.TotalCases} done | in-flight {inFlight} | now: {Trunc(current, 36)}";
            Console.Write("\r" + line.PadRight(140));

            if (completed >= p.TotalCases)
                Console.WriteLine();
        }
    }

    var runner = new Runner(client, estimator, modelToRun, maxConcurrency, Progress);

    Console.WriteLine();
    Console.WriteLine($"=== [{modelIndex}/{modelTotal}] {modelToRun} ===");
    Console.WriteLine($"Live: {live}");
    Console.WriteLine($"Endpoint: {endpoint}");
    Console.WriteLine($"Model: {modelToRun}");
    Console.WriteLine($"Measurer: {measurer.GetType().Name}");
    Console.WriteLine($"Cases: {cases.Count}");
    Console.WriteLine($"Parallelism: {maxConcurrency}");

    IReadOnlyList<AccuracyRow> rows = await runner.RunAsync(cases, live, cts.Token);
    ConsoleReport.Print(rows);

    if (allSummaries is not null)
    {
        AccuracySummary overall = AccuracyMath.Summarize(rows);

        double? text = ComputeWeightedPrecision(rows, r => GetCaseKind(r.Name) == CaseKind.Text);
        double? image = ComputeWeightedPrecision(rows, r => GetCaseKind(r.Name) == CaseKind.Image);
        double? audio = ComputeWeightedPrecision(rows, r => GetCaseKind(r.Name) == CaseKind.Audio);
        double? video = ComputeWeightedPrecision(rows, r => GetCaseKind(r.Name) == CaseKind.Video);
        double? pdf = ComputeWeightedPrecision(rows, r => GetCaseKind(r.Name) == CaseKind.Pdf);

        allSummaries.Add(new ModelSummary(
            Model: modelToRun,
            Measurer: measurer.GetType().Name,
            Text: text,
            Image: image,
            Audio: audio,
            Video: video,
            Pdf: pdf,
            Overall: overall.WeightedPrecision,
            Ran: overall.Ran,
            Skipped: overall.Skipped,
            Failed: overall.Failed
        ));
    }
}

if (allSummaries is not null)
{
    Console.WriteLine("\nAll-models summary:\n");
    Console.WriteLine("|Measurer|Model|Text|Image|Audio|Video|Pdf|Overall|>95%|Ran|Skipped|Failed|");
    Console.WriteLine("|---|---|---:|---:|---:|---:|---:|---:|:---:|---:|---:|---:|");

    foreach (ModelSummary s in allSummaries)
    {
        Console.WriteLine(
            $"|{EscapeMdCell(s.Measurer)}|{EscapeMdCell(s.Model)}|{FormatPercentMd(s.Text)}|{FormatPercentMd(s.Image)}|{FormatPercentMd(s.Audio)}|{FormatPercentMd(s.Video)}|{FormatPercentMd(s.Pdf)}|{FormatPercentMd(s.Overall)}|{GoalEmoji(s.Overall)}|{s.Ran}|{s.Skipped}|{s.Failed}|");
    }

    Console.WriteLine();
}

enum CaseKind
{
    Unknown = 0,
    Text,
    Image,
    Audio,
    Video,
    Pdf,
    Mixed,
}

sealed record ModelSummary(
    string Model,
    string Measurer,
    double? Text,
    double? Image,
    double? Audio,
    double? Video,
    double? Pdf,
    double? Overall,
    int Ran,
    int Skipped,
    int Failed
);
