using CountTokens.Content;
using CountTokens.Internal;

namespace CountTokens
{
    // Based on openai/gpt-oss-20b token counting logic (approximation).
    public sealed class OpenAiOssTokenMeasurer : ITokenMeasurer
    {
        public ValueTask<int> CountTokensAsync(string text)
        {
            int baseCount = SharedTokenizer.CountTokens(text);
            if (baseCount <= 0)
                return new ValueTask<int>(0);

            // Apply factor but handle very short texts differently
            if (baseCount < 50)
            {
                // For very short texts, use a smaller multiplier
                int adjusted = (int)Math.Ceiling(baseCount * 1.82);
                return new ValueTask<int>(Math.Max(1, adjusted));
            }

            int result = (int)Math.Ceiling(baseCount * 2.03);
            return new ValueTask<int>(Math.Max(1, result));
        }

        public ValueTask<int> CountTokensAsync(MultimodalContent content)
            => throw new NotSupportedException("This model is text-only.");
    }
}
