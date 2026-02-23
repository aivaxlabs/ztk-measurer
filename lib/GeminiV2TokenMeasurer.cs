using CountTokens.Content;
using CountTokens.Internal;

namespace CountTokens
{
    // Based on gemini-2.5-flash-lite token counting logic (approximation).
    public sealed class GeminiV2TokenMeasurer : ITokenMeasurer
    {
        public ValueTask<int> CountTokensAsync(string text)
            => new(SharedTokenizer.CountTokens(text));

        public ValueTask<int> CountTokensAsync(MultimodalContent content)
            => new(GeminiMultimodalTokenCounter.CountTokens(content));
    }
}
