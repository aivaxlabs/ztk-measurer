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
