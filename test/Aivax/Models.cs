using System.Text.Json.Serialization;

namespace CountTokens_Tester.Aivax;

internal sealed record ChatCompletionRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
    [property: JsonPropertyName("stream")] bool Stream = false,
    [property: JsonPropertyName("temperature")] double? Temperature = null,
    [property: JsonPropertyName("max_tokens")] int? MaxTokens = null
);

internal sealed record ChatMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] object Content
);

internal sealed record ChatCompletionResponse(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("model")] string? Model,
    [property: JsonPropertyName("usage")] Usage? Usage,
    [property: JsonPropertyName("choices")] IReadOnlyList<Choice>? Choices
);

internal sealed record Choice(
    [property: JsonPropertyName("index")] int? Index,
    [property: JsonPropertyName("message")] AssistantMessage? Message,
    [property: JsonPropertyName("finish_reason")] string? FinishReason
);

internal sealed record AssistantMessage(
    [property: JsonPropertyName("role")] string? Role,
    [property: JsonPropertyName("content")] string? Content
);

internal sealed record Usage(
    [property: JsonPropertyName("prompt_tokens")] int? PromptTokens,
    [property: JsonPropertyName("completion_tokens")] int? CompletionTokens,
    [property: JsonPropertyName("total_tokens")] int? TotalTokens,
    [property: JsonPropertyName("input_tokens")] int? InputTokens,
    [property: JsonPropertyName("output_tokens")] int? OutputTokens,
    [property: JsonPropertyName("cached_input_tokens")] int? CachedInputTokens = null,
    [property: JsonPropertyName("audio_tokens")] int? AudioTokens = null,
    [property: JsonPropertyName("cached_audio_input_tokens")] int? CachedAudioInputTokens = null,
    [property: JsonPropertyName("prompt_tokens_details")] PromptTokensDetails? PromptTokensDetails = null
);

internal sealed record PromptTokensDetails(
    [property: JsonPropertyName("cached_tokens")] int? CachedTokens,
    [property: JsonPropertyName("audio_tokens")] int? AudioTokens,
    // Some providers may use different naming for cached audio tokens.
    [property: JsonPropertyName("cached_audio_tokens")] int? CachedAudioTokens = null,
    [property: JsonPropertyName("cached_audio_input_tokens")] int? CachedAudioInputTokens = null
);

internal sealed record ContentPart(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string? Text = null,
    [property: JsonPropertyName("image_url")] ImageUrlPart? ImageUrl = null,
    [property: JsonPropertyName("file")] FilePart? File = null,
    [property: JsonPropertyName("input_audio")] InputAudioPart? InputAudio = null
)
{
    public static ContentPart TextPart(string text) => new("text", Text: text);

    public static ContentPart ImageDataUrl(string dataUrl, string? detail = "auto")
        => new("image_url", ImageUrl: new ImageUrlPart(dataUrl, detail));

    public static ContentPart FileDataUrl(string filename, string fileData)
        => new("file", File: new FilePart(filename, fileData));

    public static ContentPart InputAudioBase64(string base64, string format)
        => new("input_audio", InputAudio: new InputAudioPart(base64, format));
}

internal sealed record ImageUrlPart(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("detail")] string? Detail
);

internal sealed record FilePart(
    [property: JsonPropertyName("filename")] string Filename,
    [property: JsonPropertyName("file_data")] string FileData
);

internal sealed record InputAudioPart(
    [property: JsonPropertyName("data")] string Data,
    [property: JsonPropertyName("format")] string Format
);
