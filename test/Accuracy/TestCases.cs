using CountTokens_Tester.Aivax;

namespace CountTokens_Tester.Accuracy;

internal sealed record AccuracyTestCase(
    string Name,
    IReadOnlyList<ChatMessage> Messages,
    int? MaxRequestBytes = null
);

internal static class TestCases
{
    public static IReadOnlyList<AccuracyTestCase> Create(string mediaDir)
    {
        string img1 = Path.Combine(mediaDir, "img1.jpg");
        string img2 = Path.Combine(mediaDir, "img2.jpg");
        string img3 = Path.Combine(mediaDir, "img3.jpg");
        string pdf1 = Path.Combine(mediaDir, "pdf1.pdf");
        string pdf2 = Path.Combine(mediaDir, "pdf2.pdf");
        string pdf3 = Path.Combine(mediaDir, "pdf3.pdf");
        string aud1 = Path.Combine(mediaDir, "audio1.mp3");
        string aud2 = Path.Combine(mediaDir, "audio2.mp3");
        string vid1 = Path.Combine(mediaDir, "vid1.mp4");
        string vid2 = Path.Combine(mediaDir, "vid2.mp4");
        string vid3 = Path.Combine(mediaDir, "vid3.mp4");

        var cases = new List<AccuracyTestCase>
        {
            new AccuracyTestCase(
                Name: "text-short",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.TextPart("Count tokens for this simple sentence, please."),
                    })
                }
            ),

            new AccuracyTestCase(
                Name: "text-long",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.TextPart("Write a short summary of the following text, then list 5 key points. " + new string('A', 2500)),
                    })
                }
            ),

            new AccuracyTestCase(
                Name: "text-random-20k",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.TextPart("Summarize the following text in 5 bullet points:\n\n" + GenerateRandomText(targetChars: 20_000, seed: 20260222_1)),
                    })
                }
            ),

            new AccuracyTestCase(
                Name: "text-random-35k",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.TextPart("Rewrite the following text in a clearer way, keeping the same meaning:\n\n" + GenerateRandomText(targetChars: 35_000, seed: 20260222_2)),
                    })
                }
            ),

            new AccuracyTestCase(
                Name: "text-random-50k",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.TextPart("Extract 10 key points from the following text:\n\n" + GenerateRandomText(targetChars: 50_000, seed: 20260222_3)),
                    })
                }
            ),

            new AccuracyTestCase(
                Name: "text-extreme-dense-multi",
                Messages: CreateExtremeTextMessages(
                    caseName: "text-extreme-dense-multi",
                    targetChars: 40_000,
                    seed: 20260223_1,
                    minMessages: 3,
                    maxMessages: 11,
                    flavor: ExtremeTextFlavor.Dense
                )
            ),

            new AccuracyTestCase(
                Name: "text-extreme-code-multi",
                Messages: CreateExtremeTextMessages(
                    caseName: "text-extreme-code-multi",
                    targetChars: 46_000,
                    seed: 20260223_2,
                    minMessages: 2,
                    maxMessages: 9,
                    flavor: ExtremeTextFlavor.Code
                )
            ),

            new AccuracyTestCase(
                Name: "text-extreme-unicode-multi",
                Messages: CreateExtremeTextMessages(
                    caseName: "text-extreme-unicode-multi",
                    targetChars: 40_000,
                    seed: 20260223_3,
                    minMessages: 3,
                    maxMessages: 12,
                    flavor: ExtremeTextFlavor.Unicode
                )
            ),

            new AccuracyTestCase(
                Name: "text-extreme-whitespace-multi",
                Messages: CreateExtremeTextMessages(
                    caseName: "text-extreme-whitespace-multi",
                    targetChars: 45_000,
                    seed: 20260223_4,
                    minMessages: 2,
                    maxMessages: 8,
                    flavor: ExtremeTextFlavor.Whitespace
                )
            ),

            new AccuracyTestCase(
                Name: "text-extreme-json-multi",
                Messages: CreateExtremeTextMessages(
                    caseName: "text-extreme-json-multi",
                    targetChars: 38_000,
                    seed: 20260223_5,
                    minMessages: 2,
                    maxMessages: 10,
                    flavor: ExtremeTextFlavor.Json
                )
            ),

            new AccuracyTestCase(
                Name: "image-1",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.ImageDataUrl(ToDataUrl(img1, "image/jpeg"))
                    })
                },
                MaxRequestBytes: 8_000_000
            ),

            new AccuracyTestCase(
                Name: "image-1-detail-low",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.ImageDataUrl(ToDataUrl(img1, "image/jpeg"), detail: "low")
                    })
                },
                MaxRequestBytes: 8_000_000
            ),

            new AccuracyTestCase(
                Name: "image-1-detail-auto-null",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.ImageDataUrl(ToDataUrl(img1, "image/jpeg"), detail: null)
                    })
                },
                MaxRequestBytes: 8_000_000
            ),

            new AccuracyTestCase(
                Name: "image-1-detail-high",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.ImageDataUrl(ToDataUrl(img1, "image/jpeg"), detail: "high")
                    })
                },
                MaxRequestBytes: 8_000_000
            ),

            new AccuracyTestCase(
                Name: "image-2",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.TextPart("Describe the image briefly in Portuguese."),
                        ContentPart.ImageDataUrl(ToDataUrl(img2, "image/jpeg"))
                    })
                },
                MaxRequestBytes: 3_000_000
            ),

            new AccuracyTestCase(
                Name: "image-3",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.TextPart("Describe the image briefly."),
                        ContentPart.ImageDataUrl(ToDataUrl(img3, "image/jpeg"))
                    })
                },
                MaxRequestBytes: 1_000_000
            ),

            new AccuracyTestCase(
                Name: "image-4-two-images",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.TextPart("Compare the two images and list 5 differences."),
                        ContentPart.ImageDataUrl(ToDataUrl(img2, "image/jpeg")),
                        ContentPart.ImageDataUrl(ToDataUrl(img3, "image/jpeg")),
                    })
                },
                MaxRequestBytes: 4_000_000
            ),

            new AccuracyTestCase(
                Name: "pdf-1",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.TextPart("Summarize this PDF briefly."),
                        ContentPart.FileDataUrl("pdf1.pdf", ToDataUrl(pdf1, "application/pdf"))
                    })
                },
                MaxRequestBytes: 8_000_000
            ),

            new AccuracyTestCase(
                Name: "pdf-2",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.TextPart("Extract 3 bullet points from this PDF."),
                        ContentPart.FileDataUrl("pdf2.pdf", ToDataUrl(pdf2, "application/pdf"))
                    })
                },
                MaxRequestBytes: 15_000_000
            ),

            new AccuracyTestCase(
                Name: "pdf-3",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.TextPart("Extract 5 bullet points from this PDF."),
                        ContentPart.FileDataUrl("pdf3.pdf", ToDataUrl(pdf3, "application/pdf"))
                    })
                },
                MaxRequestBytes: 20_000_000
            ),

            new AccuracyTestCase(
                Name: "pdf-4-two-pdfs",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.TextPart("Summarize each PDF separately in 3 bullet points."),
                        ContentPart.FileDataUrl("pdf1.pdf", ToDataUrl(pdf1, "application/pdf")),
                        ContentPart.FileDataUrl("pdf2.pdf", ToDataUrl(pdf2, "application/pdf")),
                    })
                },
                MaxRequestBytes: 25_000_000
            ),

            new AccuracyTestCase(
                Name: "audio-1",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.TextPart("Tell me what is said in this audio."),
                        ContentPart.InputAudioBase64(ToBase64(aud1), "mp3")
                    })
                },
                MaxRequestBytes: 12_000_000
            ),

            new AccuracyTestCase(
                Name: "audio-2",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.TextPart("In one sentence, what is the main topic of this audio?"),
                        ContentPart.InputAudioBase64(ToBase64(aud2), "mp3")
                    })
                },
                MaxRequestBytes: 12_000_000
            ),

            new AccuracyTestCase(
                Name: "audio-3-two-audios",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.TextPart("For each audio, provide a 1-sentence summary."),
                        ContentPart.InputAudioBase64(ToBase64(aud1), "mp3"),
                        ContentPart.InputAudioBase64(ToBase64(aud2), "mp3"),
                    })
                },
                MaxRequestBytes: 25_000_000
            ),

            new AccuracyTestCase(
                Name: "video-1",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.TextPart("Describe what is happening in this video."),
                        ContentPart.FileDataUrl("vid1.mp4", ToDataUrl(vid1, "video/mp4"))
                    })
                },
                MaxRequestBytes: 12_000_000
            ),

            new AccuracyTestCase(
                Name: "video-2",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.TextPart("Summarize this video in 3 bullet points."),
                        ContentPart.FileDataUrl("vid2.mp4", ToDataUrl(vid2, "video/mp4"))
                    })
                },
                MaxRequestBytes: 35_000_000
            ),

            new AccuracyTestCase(
                Name: "video-3",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.TextPart("Summarize this video in 5 bullet points."),
                        ContentPart.FileDataUrl("vid3.mp4", ToDataUrl(vid3, "video/mp4"))
                    })
                },
                MaxRequestBytes: 35_000_000
            ),

            new AccuracyTestCase(
                Name: "video-4-video-plus-image",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.TextPart("Describe the image and the video, then tell if they seem related."),
                        ContentPart.ImageDataUrl(ToDataUrl(img2, "image/jpeg")),
                        ContentPart.FileDataUrl("vid1.mp4", ToDataUrl(vid1, "video/mp4")),
                    })
                },
                MaxRequestBytes: 16_000_000
            ),

            new AccuracyTestCase(
                Name: "mixed-all",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.TextPart("Analyze all attachments. Give a short summary for each (image, pdf, audio, video)."),
                        ContentPart.ImageDataUrl(ToDataUrl(img1, "image/jpeg")),
                        ContentPart.FileDataUrl("pdf1.pdf", ToDataUrl(pdf1, "application/pdf")),
                        ContentPart.InputAudioBase64(ToBase64(aud1), "mp3"),
                        ContentPart.FileDataUrl("vid1.mp4", ToDataUrl(vid1, "video/mp4")),
                    })
                },
                MaxRequestBytes: 30_000_000
            ),

            new AccuracyTestCase(
                Name: "mixed-all-2",
                Messages: new []
                {
                    new ChatMessage("user", new []
                    {
                        ContentPart.TextPart("Analyze all attachments. Provide a short summary for each item."),
                        ContentPart.ImageDataUrl(ToDataUrl(img2, "image/jpeg")),
                        ContentPart.ImageDataUrl(ToDataUrl(img3, "image/jpeg")),
                        ContentPart.FileDataUrl("pdf2.pdf", ToDataUrl(pdf2, "application/pdf")),
                        ContentPart.InputAudioBase64(ToBase64(aud2), "mp3"),
                        ContentPart.FileDataUrl("vid2.mp4", ToDataUrl(vid2, "video/mp4")),
                    })
                },
                MaxRequestBytes: 60_000_000
            )

        };

        return cases;
    }

    private static string ToDataUrl(string path, string mime)
    {
        byte[] bytes = File.ReadAllBytes(path);
        return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
    }

    private static string ToBase64(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        return Convert.ToBase64String(bytes);
    }

    private static string GenerateRandomText(int targetChars, int seed)
    {
        if (targetChars <= 0)
            return string.Empty;

        var rng = new Random(seed);
        var sb = new System.Text.StringBuilder(capacity: targetChars + 256);

        int paragraphs = Math.Max(3, targetChars / 1200);
        for (int p = 0; p < paragraphs && sb.Length < targetChars; p++)
        {
            int sentences = rng.Next(3, 10);

            for (int s = 0; s < sentences && sb.Length < targetChars; s++)
            {
                int words = rng.Next(8, 22);
                for (int w = 0; w < words && sb.Length < targetChars; w++)
                {
                    if (w > 0)
                        sb.Append(' ');

                    sb.Append(GenerateWord(rng));

                    if (rng.NextDouble() < 0.08)
                        sb.Append(',');
                    if (rng.NextDouble() < 0.02)
                        sb.Append(';');
                }

                sb.Append('.');
                sb.Append(' ');

                if (rng.NextDouble() < 0.12)
                    sb.Append("\n");
            }

            sb.Append("\n\n");
        }

        if (sb.Length > targetChars)
            sb.Length = targetChars;

        return sb.ToString();
    }

    private enum ExtremeTextFlavor
    {
        Dense = 1,
        Code = 2,
        Unicode = 3,
        Whitespace = 4,
        Json = 5,
    }

    private static IReadOnlyList<ChatMessage> CreateExtremeTextMessages(string caseName, int targetChars, int seed, int minMessages, int maxMessages, ExtremeTextFlavor flavor)
    {
        var rng = new Random(seed);

        int messageCount = rng.Next(Math.Max(1, minMessages), Math.Max(minMessages, maxMessages) + 1);
        string payload = GenerateExtremeText(targetChars, seed, flavor);
        string[] chunks = SplitIntoChunks(payload, messageCount);

        var messages = new ChatMessage[chunks.Length];
        for (int i = 0; i < chunks.Length; i++)
        {
            string text = i == 0
                ? $"Case '{caseName}': process the following text.\n\n{chunks[i]}"
                : chunks[i];

            messages[i] = new ChatMessage("user", new[] { ContentPart.TextPart(text) });
        }

        return messages;
    }

    private static string[] SplitIntoChunks(string text, int chunks)
    {
        if (chunks <= 1 || string.IsNullOrEmpty(text))
            return new[] { text };

        int total = text.Length;
        int approx = Math.Max(1, total / chunks);

        var result = new string[chunks];
        int start = 0;

        for (int i = 0; i < chunks; i++)
        {
            if (start >= total)
            {
                result[i] = string.Empty;
                continue;
            }

            int end = (i == chunks - 1) ? total : Math.Min(total, start + approx);
            if (end < total)
            {
                int best = FindSplitPoint(text, end, window: 256);
                if (best > start)
                    end = best;
            }

            result[i] = text[start..end];
            start = end;
        }

        return result;
    }

    private static int FindSplitPoint(string text, int around, int window)
    {
        int total = text.Length;
        int left = Math.Max(0, around - window);
        int right = Math.Min(total, around + window);

        for (int i = around; i < right; i++)
        {
            char c = text[i];
            if (char.IsWhiteSpace(c))
                return i;
        }

        for (int i = around; i > left; i--)
        {
            char c = text[i - 1];
            if (char.IsWhiteSpace(c))
                return i;
        }

        return around;
    }

    private static string GenerateExtremeText(int targetChars, int seed, ExtremeTextFlavor flavor)
    {
        var rng = new Random(seed);
        var sb = new System.Text.StringBuilder(capacity: targetChars + 1024);

        void AppendRandomPreamble()
        {
            if (rng.NextDouble() < 0.35)
                sb.Append("\n---\n");
            if (rng.NextDouble() < 0.25)
                sb.Append("\n# Section ").Append(rng.Next(1, 9999)).Append("\n");
        }

        while (sb.Length < targetChars)
        {
            AppendRandomPreamble();

            switch (flavor)
            {
                case ExtremeTextFlavor.Dense:
                    sb.Append(GenerateRandomText(targetChars: Math.Min(2400, targetChars - sb.Length + 512), seed: rng.Next()));
                    if (rng.NextDouble() < 0.50)
                        sb.Append("\n").Append(rng.NextInt64()).Append(" ").Append(rng.NextDouble().ToString("0.0000000000"));
                    break;

                case ExtremeTextFlavor.Code:
                    sb.Append("```csharp\n");
                    sb.Append("public static class X").Append(rng.Next(1, 9999)).Append(" {\n");
                    for (int i = 0; i < 20 && sb.Length < targetChars; i++)
                    {
                        sb.Append("  public static int F").Append(rng.Next(1, 9999)).Append("(int a, int b) => (a ^ b) + ").Append(rng.Next(0, 1024)).Append(";\n");
                    }
                    sb.Append("}\n```");
                    sb.Append("\n");
                    break;

                case ExtremeTextFlavor.Unicode:
                    sb.Append("Português: amanhã, ação, coração. ");
                    sb.Append("Deutsch: übermäßig, Größe. ");
                    sb.Append("日本語: これはテストです。 ");
                    sb.Append("中文: 你好，世界。 ");
                    sb.Append("한국어: 안녕하세요. ");
                    sb.Append("Emoji: 😀😅🚀✨🔥💾🔒📦🧪\n");
                    if (rng.NextDouble() < 0.55)
                        sb.Append(GenerateRandomText(targetChars: 1200, seed: rng.Next()));
                    break;

                case ExtremeTextFlavor.Whitespace:
                    sb.Append("Line with tabs\t\t\tand spaces\n");
                    for (int i = 0; i < 80 && sb.Length < targetChars; i++)
                    {
                        int indent = rng.Next(0, 40);
                        sb.Append(' ', indent);
                        sb.Append(GenerateWord(rng));
                        if (rng.NextDouble() < 0.35)
                            sb.Append(' ').Append(GenerateWord(rng));
                        sb.Append("\n");
                        if (rng.NextDouble() < 0.10)
                            sb.Append("\n\n");
                    }
                    break;

                case ExtremeTextFlavor.Json:
                    sb.Append("{\n  \"events\": [\n");
                    for (int i = 0; i < 60 && sb.Length < targetChars; i++)
                    {
                        sb.Append("    {\"id\":\"").Append(rng.Next(1, 1_000_000)).Append("\",\"ts\":\"");
                        sb.Append(DateTimeOffset.FromUnixTimeSeconds(1_700_000_000 + rng.Next(0, 10_000_000)).ToString("O"));
                        sb.Append("\",\"ok\":").Append(rng.NextDouble() < 0.93 ? "true" : "false");
                        sb.Append(",\"msg\":\"").Append(GenerateWord(rng)).Append(' ').Append(GenerateWord(rng)).Append("\"}");
                        sb.Append(i < 59 ? ",\n" : "\n");
                    }
                    sb.Append("  ]\n}\n");
                    break;
            }

            sb.Append("\n");
        }

        if (sb.Length > targetChars)
            sb.Length = targetChars;

        return sb.ToString();
    }

    private static string GenerateWord(Random rng)
    {
        const string letters = "abcdefghijklmnopqrstuvwxyz";

        int len = rng.Next(2, 13);
        Span<char> buf = len <= 64 ? stackalloc char[len] : new char[len];

        for (int i = 0; i < len; i++)
        {
            char c = letters[rng.Next(letters.Length)];
            if (i == 0 && rng.NextDouble() < 0.20)
                c = char.ToUpperInvariant(c);
            buf[i] = c;
        }

        if (rng.NextDouble() < 0.03)
        {
            int dashPos = rng.Next(1, len - 1);
            buf[dashPos] = '-';
        }

        return new string(buf);
    }
}
