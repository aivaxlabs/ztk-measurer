using CountTokens.Content;
using CountTokens.Internal;

namespace CountTokens
{
    // Based on gemini-3-flash token counting logic (approximation).
    public sealed class GeminiV3TokenMeasurer : ITokenMeasurer
    {
        public ValueTask<int> CountTokensAsync(string text)
            => new(SharedTokenizer.CountTokens(text));

        public ValueTask<int> CountTokensAsync(MultimodalContent content)
            => new(GeminiMultimodalTokenCounter.CountTokens(content));
    }
}
