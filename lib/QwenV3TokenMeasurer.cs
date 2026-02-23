using CountTokens.Content;
using CountTokens.Internal;

namespace CountTokens
{
    // Based on qwen/qwen3-32b token counting logic (approximation).
    public sealed class QwenV3TokenMeasurer : ITokenMeasurer
    {
        public ValueTask<int> CountTokensAsync(string text)
        {
            int baseTokens = SharedTokenizer.CountTokens(text);
            if (baseTokens <= 0)
                return new ValueTask<int>(0);

            double factor = baseTokens < 50 ? 3.75d : 2.09d;

            if (ContainsTripleBacktick(text))
                factor *= 1.12d;
            else if (IsJsonHeavy(text))
                factor *= 1.33d;

            int adjusted = (int)Math.Ceiling(baseTokens * factor);
            return new ValueTask<int>(Math.Max(1, adjusted));
        }

        public ValueTask<int> CountTokensAsync(MultimodalContent content)
            => throw new NotSupportedException("This model is text-only.");

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
    }
}
