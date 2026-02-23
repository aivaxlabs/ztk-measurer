using CountTokens.Content;

namespace CountTokens
{
    public interface ITokenMeasurer
    {
        public ValueTask<int> CountTokensAsync(string text);
        public ValueTask<int> CountTokensAsync(MultimodalContent content);
    }
}
