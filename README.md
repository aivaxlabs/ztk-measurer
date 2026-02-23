# ztk-measurer

## Intent

This repository provides a fast, allocation-conscious token estimator library plus a live accuracy harness.
The goal is to estimate prompt/input token usage (text and supported multimodal inputs) for multiple LLM families,
and then validate those estimates against real provider usage metrics.

## What’s in the repo

- `lib/`
  - CountTokens: token estimation library
  - Model-specific measurers (Gemini / Claude / OpenAI / Llama / Qwen)
  - Multimodal content helpers (image/audio/video/pdf) and low-allocation utilities
- `test/`
  - CountTokens_Tester: live accuracy runner (calls an OpenAI-compatible endpoint)
  - Aivax client + response models (parses usage and prompt token breakdown details)
  - Accuracy suite cases (text, image, pdf, audio, video, mixed; some models skip unsupported modalities)

## Methodology

For each test case:

1. The estimator computes `EstimatedPromptTokens` for the input messages.
2. In live mode, the runner sends the same messages to the endpoint and reads usage fields.
3. The suite computes `Diff = Estimated - Actual` and a per-case precision.

### Actual input tokens (suite definition)

- `input_tokens + cached_input_tokens + audio_tokens + cached_audio_input_tokens`
- If a provider only exposes `prompt_tokens` (and `prompt_tokens_details` is only a breakdown), the suite uses `prompt_tokens`
  and does **not** double-count the breakdown.

### Precision metrics

- **Weighted precision**: $1 - \frac{\sum |diff|}{\sum actual}$ across included cases
- **Mean precision**: average of per-case precision across included cases

## Results

A sample run report is checked in at [test-results.txt](test-results.txt).

|Measurer|Model|Text|Image|Audio|Video|Pdf|Overall|>95%|Ran|Skipped|Failed|
|---|---|---:|---:|---:|---:|---:|---:|:---:|---:|---:|---:|
|GeminiV2TokenMeasurer|@google/gemini-2.5-flash-lite|100%|100%|100%|100%|100%|100%|✅|30|0|0|
|GeminiV3TokenMeasurer|@google/gemini-3-flash|100%|99%|100%|97%|100%|99%|✅|30|0|0|
|ClaudeV4TokenMeasurer|@anthropic/claude-4.5-haiku|99%|99%|—|—|—|99%|✅|17|13|0|
|LlamaV3TokenMeasurer|@metaai/llama-3.1-8b|98%|—|—|—|—|98%|✅|10|20|0|
|LlamaV4TokenMeasurer|@metaai/llama-4-scout-17b-16e|100%|100%|—|—|—|100%|✅|17|13|0|
|OpenAiV5TokenMeasurer|@openai/gpt-5-nano|100%|99%|—|—|99%|100%|✅|21|9|0|
|OpenAiV4_1TokenMeasurer|@openai/gpt-4.1-nano|100%|69%|—|—|—|98%|✅|17|13|0|
|OpenAiOssTokenMeasurer|@openai/gpt-oss-20b|99%|—|—|—|—|99%|✅|10|20|0|
|QwenV3TokenMeasurer|@qwen/qwen3-32b|100%|—|—|—|—|100%|✅|10|20|0|

Recent all-models summary (from that report):

- Most models achieve $\ge 95\%$ overall and (where applicable) $\ge 95\%$ by modality.
- Known remaining outlier: `@openai/gpt-4.1-nano` / image `detail=high` (see **Known issues**).

## How to build

Prerequisites:

- .NET SDK 10 (TargetFramework: `net10.0`)

Build:

```bash
dotnet build
```

## How to run the accuracy suite

Notes:

- Live mode requires an API key for the endpoint.
- Default endpoint: `https://inference.aivax.net/v1/chat/completions`
- Media test inputs live under `test/Media` and are copied to the output directory.

### Environment

- `AIVAX_API_KEY`
  - Bearer token required for `--live`
  - You can use any OpenAI-compatible endpoint, but the test suite is designed around AIVAX’s usage reporting format.
- `AIVAX_DEBUG_USAGE` (optional)
  - If set (any value), prints parsed usage fields per case for debugging

### Run all known models

```bash
dotnet run --project test/CountTokens_Tester.csproj -- --live --all-models > test-results.txt
```

### Run a single model

```bash
dotnet run --project test/CountTokens_Tester.csproj -- --live --model @openai/gpt-5-nano
```

### Run a single case

```bash
dotnet run --project test/CountTokens_Tester.csproj -- --live --model @openai/gpt-4.1-nano --case image-4-two-images
```

### Override endpoint

```bash
dotnet run --project test/CountTokens_Tester.csproj -- --live --endpoint https://your-host/v1/chat/completions
```

### Convenience script

- [test.sh](test.sh) runs the all-models suite and writes `../test-results.txt`.

## Known issues / limitations

1. **`@openai/gpt-4.1-nano` image `detail=high`**

   In the current sample report, the case `image-1-detail-high` returns the same `ActualPromptTokens` as low/auto
   from the endpoint usage, while the estimator assumes a higher token cost for `high` detail.
   This makes that specific case look wrong and can drag the image modality score for that model.

   Next step options:

   - Treat `high` as `auto` for `@openai/gpt-4.1-nano` in the request builder (if the backend ignores `high` anyway), or
   - Update the estimator for this model to match the observed backend behavior, or
   - Split the test into “requested detail” vs “effective detail” if the provider normalizes it.

## License

MIT License. See [LICENSE](LICENSE).

## Next steps

- Resolve the `@openai/gpt-4.1-nano` `detail=high` mismatch (request vs backend behavior) and re-run the suite.
- Add a deterministic, offline-only accuracy mode (golden token counts) to reduce dependency on live endpoints.
- Expand multimodal edge cases while keeping performance constraints (avoid heavy parsing, keep allocations low).

## Citation

If you use this project in research or tooling, cite it as:

> ZTK Measurer. Repository: ztk-measurer. (Accessed 2026-02-23).

BibTeX:

```bibtex
@misc{ztk_measurer_2026,
  title        = {aivaxlabs/ztk-measurer},
  howpublished = {Software repository},
  year         = {2026},
  note         = {Accessed 2026-02-23}
}
```
