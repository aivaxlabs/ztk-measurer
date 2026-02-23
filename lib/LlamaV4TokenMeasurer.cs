using CountTokens.Content;
using CountTokens.Internal;

namespace CountTokens
{
    // Based on meta/llama-4-scout-17b-16e token counting logic (approximation).
    public sealed class LlamaV4TokenMeasurer : ITokenMeasurer
    {
        public ValueTask<int> CountTokensAsync(string text)
            => new(SharedTokenizer.CountTokens(text));

        public ValueTask<int> CountTokensAsync(MultimodalContent content)
        {
            ArgumentNullException.ThrowIfNull(content);

            int count = content switch
            {
                ImageContent image => GeminiMultimodalTokenCounter.CountImageTokens(image),
                _ => throw new NotSupportedException("This model does not support this modality."),
            };

            return new ValueTask<int>(count);
        }
    }
}
