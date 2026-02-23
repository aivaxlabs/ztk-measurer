using CountTokens.Content;
using CountTokens.Internal;

namespace CountTokens
{
    // Based on gemini-3-flash token counting logic (approximation).
    public sealed class GeminiV3TokenMeasurer : ITokenMeasurer
    {
        public ValueTask<int> CountTokensAsync(string text)
            => new(CountGeminiTextTokens(text));

        public ValueTask<int> CountTokensAsync(MultimodalContent content)
            => new(GeminiV3MultimodalTokenCounter.CountTokens(content));

        private static int CountGeminiTextTokens(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            if (TryEstimateTrailingRun(text, out int estimate))
                return estimate;

            int baseTokens = SharedTokenizer.CountTokens(text);
            if (baseTokens <= 0)
                return 0;

            double factor = GetGeminiContentFactor(text);
            if (factor <= 0d || factor == 1d)
                return baseTokens;

            return (int)Math.Ceiling(baseTokens * factor);
        }

        private static double GetGeminiContentFactor(string text)
        {
            if (ContainsTripleBacktick(text))
                return 1.26d;

            if (IsJsonHeavy(text))
                return 1.40d;

            return 1d;
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

        private static bool TryEstimateTrailingRun(string text, out int tokens)
        {
            tokens = 0;

            if (text.Length < 1024)
                return false;

            int i = text.Length - 1;
            char c = text[i];

            int run = 1;
            while (i > 0 && text[i - 1] == c)
            {
                run++;
                i--;
            }

            if (run < 512)
                return false;

            int nonRunChars = text.Length - run;
            int estimated = (int)Math.Ceiling(nonRunChars / 4.5d) + (int)Math.Ceiling(run / 16d);
            tokens = Math.Max(1, estimated);
            return true;
        }
    }
}
