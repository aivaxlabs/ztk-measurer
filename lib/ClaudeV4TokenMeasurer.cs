using CountTokens.Content;
using CountTokens.Internal;

namespace CountTokens
{
    // Based on anthropic/claude-4.5-haiku token counting logic (approximation).
    public sealed class ClaudeV4TokenMeasurer : ITokenMeasurer
    {
        public ValueTask<int> CountTokensAsync(string text)
        {
            int baseTokens = SharedTokenizer.CountTokens(text);
            if (baseTokens <= 0)
                return new ValueTask<int>(0);

            double factor = 1.24d;

            if (ContainsTripleBacktick(text))
                factor *= 0.88d;
            else if (IsJsonHeavy(text))
                factor *= 0.86d;

            int estimated = (int)Math.Ceiling(baseTokens * factor);

            if (baseTokens < 50)
                estimated += 4;

            if (TryGetTrailingRunLength(text, minRun: 512, out int runLen))
            {
                // Claude's tokenization for huge repeated runs diverges from o200k_base.
                // This approximates the gap without substring allocations.
                estimated += (int)Math.Ceiling(runLen / 5.5d);
            }

            return new ValueTask<int>(Math.Max(1, estimated));
        }

        public ValueTask<int> CountTokensAsync(MultimodalContent content)
        {
            ArgumentNullException.ThrowIfNull(content);

            int count = content switch
            {
                ImageContent image => CountClaudeImageTokens(image),
                PdfContent => throw new NotSupportedException("This model does not support this modality."),
                _ => throw new NotSupportedException("This model does not support this modality."),
            };

            return new ValueTask<int>(count);
        }

        private static int CountClaudeImageTokens(ImageContent image)
        {
            byte[]? bytes = image.Contents;
            if (bytes is null || bytes.Length == 0)
                return 0;

            if (ImageHeaderParser.TryGetSize(bytes, out int width, out int height))
            {
                (int resizedW, int resizedH) = MediaHelpers.ResizeToMaxSide(width, height, 1568);
                long pixels = (long)resizedW * resizedH;

                int approx = (int)Math.Ceiling(pixels / 760d);
                return Math.Clamp(approx, 900, 1600);
            }

            return 900;
        }

        private static bool ContainsTripleBacktick(string text)
            => text.AsSpan().IndexOf("```".AsSpan(), StringComparison.Ordinal) >= 0;

        private static bool IsJsonHeavy(string text)
        {
            int len = Math.Min(text.Length, 4096);
            if (len < 512)
                return false;

            int quotes = 0;
            int colons = 0;
            int braces = 0;
            int brackets = 0;

            ReadOnlySpan<char> span = text.AsSpan(0, len);
            for (int i = 0; i < span.Length; i++)
            {
                switch (span[i])
                {
                    case '"': quotes++; break;
                    case ':': colons++; break;
                    case '{':
                    case '}': braces++; break;
                    case '[':
                    case ']': brackets++; break;
                }

                if (quotes >= 24 && colons >= 12 && (braces + brackets) >= 16)
                    return true;
            }

            return false;
        }

        private static bool TryGetTrailingRunLength(string text, int minRun, out int run)
        {
            run = 0;
            if (string.IsNullOrEmpty(text) || text.Length < minRun)
                return false;

            int i = text.Length - 1;
            char c = text[i];

            int count = 1;
            while (i > 0 && text[i - 1] == c)
            {
                count++;
                i--;
                if (count >= minRun)
                {
                    run = count;
                    // Continue counting to get full run length.
                    while (i > 0 && text[i - 1] == c)
                    {
                        run++;
                        i--;
                    }
                    return true;
                }
            }

            return false;
        }
    }
}
