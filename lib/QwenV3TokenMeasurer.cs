using CountTokens.Content;
using CountTokens.Internal;

namespace CountTokens
{
    // Based on qwen/qwen3-32b token counting logic (approximation).
    public sealed class QwenV3TokenMeasurer : ITokenMeasurer
    {
        public ValueTask<int> CountTokensAsync(string text)
            => new(SharedTokenizer.CountTokens(text));

        public ValueTask<int> CountTokensAsync(MultimodalContent content)
            => throw new NotSupportedException("This model is text-only.");
    }
}
