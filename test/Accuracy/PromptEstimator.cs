using CountTokens;
using CountTokens.Content;
using CountTokens_Tester.Aivax;

namespace CountTokens_Tester.Accuracy;

internal sealed class PromptEstimator
{
    private readonly ITokenMeasurer _measurer;
    private readonly int _perMessageOverheadTokens;

    public PromptEstimator(ITokenMeasurer measurer, int perMessageOverheadTokens = 0)
    {
        _measurer = measurer ?? throw new ArgumentNullException(nameof(measurer));
        _perMessageOverheadTokens = Math.Max(0, perMessageOverheadTokens);
    }

    public async ValueTask<int> EstimateAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messages);

        int total = 0;

        foreach (ChatMessage msg in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            total += _perMessageOverheadTokens;

            switch (msg.Content)
            {
                case string s:
                    total += await _measurer.CountTokensAsync(s);
                    break;

                case IReadOnlyList<ContentPart> parts:
                    int imageCountInMessage = 0;
                    foreach (ContentPart p in parts)
                    {
                        if (p.Type == "image_url")
                            imageCountInMessage++;
                    }

                    foreach (ContentPart part in parts)
                    {
                        total += await EstimatePartAsync(part, imageCountInMessage);
                    }
                    break;

                default:
                    // Unknown content type; we can't estimate reliably.
                    break;
            }
        }

        return total;
    }

    private async ValueTask<int> EstimatePartAsync(ContentPart part, int imageCountInMessage)
    {
        if (part.Type == "text")
            return await _measurer.CountTokensAsync(part.Text ?? string.Empty);

        if (part.Type == "image_url" && part.ImageUrl?.Url is { } url)
        {
            if (TryParseDataUrl(url, out string mime, out byte[] bytes))
            {
                string ext = GuessImageExtensionFromMime(mime) ?? "jpg";
                var detail = ParseImageDetail(part.ImageUrl.Detail);

                if (detail == ImageDetailLevel.Auto && imageCountInMessage > 1 && _measurer is OpenAiV4_1TokenMeasurer)
                    detail = ImageDetailLevel.Low;

                return await _measurer.CountTokensAsync(new ImageContent(bytes, ext, detail));
            }
            return 0;
        }

        if (part.Type == "file" && part.File?.FileData is { } fileData)
        {
            if (!TryParseDataUrl(fileData, out string mime, out byte[] bytes))
                return 0;

            if (mime.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                return await _measurer.CountTokensAsync(new PdfContent(bytes));

            if (mime.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                string ext = GuessExtensionFromMime(mime) ?? "mp4";
                return await _measurer.CountTokensAsync(new VideoContent(bytes, ext));
            }

            if (mime.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
            {
                string ext = GuessExtensionFromMime(mime) ?? "mp3";
                return await _measurer.CountTokensAsync(new AudioContent(bytes, ext));
            }

            return await _measurer.CountTokensAsync(new MultimodalBlob(bytes));
        }

        if (part.Type == "input_audio" && part.InputAudio is { } audio)
        {
            if (string.IsNullOrWhiteSpace(audio.Data))
                return 0;

            byte[] bytes;
            try { bytes = Convert.FromBase64String(audio.Data); }
            catch { return 0; }

            return await _measurer.CountTokensAsync(new AudioContent(bytes, audio.Format));
        }

        return 0;
    }

    private static bool TryParseDataUrl(string dataUrl, out string mime, out byte[] bytes)
    {
        mime = string.Empty;
        bytes = Array.Empty<byte>();

        if (string.IsNullOrWhiteSpace(dataUrl))
            return false;

        // data:<mime>;base64,<payload>
        const string prefix = "data:";
        if (!dataUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        int semi = dataUrl.IndexOf(';', prefix.Length);
        int comma = dataUrl.IndexOf(',', prefix.Length);
        if (comma < 0)
            return false;

        if (semi > 0 && semi < comma)
            mime = dataUrl[prefix.Length..semi];
        else
            mime = dataUrl[prefix.Length..comma];

        string payload = dataUrl[(comma + 1)..];
        try
        {
            bytes = Convert.FromBase64String(payload);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? GuessExtensionFromMime(string mime)
    {
        return mime.ToLowerInvariant() switch
        {
            "video/mp4" => "mp4",
            "video/webm" => "webm",
            "audio/mpeg" => "mp3",
            "audio/mp3" => "mp3",
            "audio/wav" => "wav",
            "audio/x-wav" => "wav",
            _ => null
        };
    }

    private static string? GuessImageExtensionFromMime(string mime)
    {
        return mime.ToLowerInvariant() switch
        {
            "image/jpeg" => "jpg",
            "image/jpg" => "jpg",
            "image/png" => "png",
            "image/webp" => "webp",
            "image/gif" => "gif",
            _ => null
        };
    }

    private static ImageDetailLevel ParseImageDetail(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
            return ImageDetailLevel.Auto;

        return detail.Trim().ToLowerInvariant() switch
        {
            "low" => ImageDetailLevel.Low,
            "high" => ImageDetailLevel.High,
            "auto" => ImageDetailLevel.Auto,
            _ => ImageDetailLevel.Auto,
        };
    }

    private sealed class MultimodalBlob : MultimodalContent
    {
        public MultimodalBlob(byte[] bytes) => Contents = bytes;
    }
}
