using CountTokens.Content;

namespace CountTokens
{
    public sealed class GeminiV2TokenMeasurer : ITokenMeasurer
    {
        private static readonly GeminiV2TokenMeasurer Impl = new();

        public ValueTask<int> CountTokensAsync(string text) => Impl.CountTokensAsync(text);

        public ValueTask<int> CountTokensAsync(MultimodalContent content) => Impl.CountTokensAsync(content);
    }
}
