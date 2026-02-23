using CountTokens.Content;
using CountTokens.Internal;

namespace CountTokens
{
    // Based on anthropic/claude-4.5-haiku token counting logic (approximation).
    public sealed class ClaudeV4TokenMeasurer : ITokenMeasurer
    {
        public ValueTask<int> CountTokensAsync(string text)
            => new(SharedTokenizer.CountTokens(text));

        public ValueTask<int> CountTokensAsync(MultimodalContent content)
        {
            ArgumentNullException.ThrowIfNull(content);

            int count = content switch
            {
                ImageContent image => GeminiMultimodalTokenCounter.CountImageTokens(image),
                PdfContent pdf => GeminiMultimodalTokenCounter.CountPdfTokens(pdf),
                _ => throw new NotSupportedException("This model does not support this modality."),
            };

            return new ValueTask<int>(count);
        }
    }
}
