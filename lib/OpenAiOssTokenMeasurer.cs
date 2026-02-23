using CountTokens.Content;
using CountTokens.Internal;

namespace CountTokens
{
    // Based on openai/gpt-oss-20b token counting logic (approximation).
    public sealed class OpenAiOssTokenMeasurer : ITokenMeasurer
    {
        public ValueTask<int> CountTokensAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new ValueTask<int>(0);

            int baseCount = SharedTokenizer.CountTokens(text);
            if (baseCount <= 0)
                return new ValueTask<int>(0);

            int result = baseCount < 50
                ? (int)Math.Ceiling(baseCount * 1.82)
                : (int)Math.Ceiling(baseCount * 2.03);

            // Empirical overhead for smaller prompts on this provider/model.
            // Keeps large prompts unaffected (overhead becomes negligible / zero).
            int overhead = result switch
            {
                < 100 => 143,
                < 1000 => 132,
                _ => 0,
            };

            return new ValueTask<int>(Math.Max(1, result + overhead));
        }

        public ValueTask<int> CountTokensAsync(MultimodalContent content)
            => throw new NotSupportedException("This model is text-only.");
    }
}
