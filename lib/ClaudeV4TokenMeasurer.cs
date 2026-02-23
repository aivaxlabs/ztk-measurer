using CountTokens.Content;
using CountTokens.Internal;

namespace CountTokens
{
    // Based on anthropic/claude-4.5-haiku token counting logic (approximation).
    public sealed class ClaudeV4TokenMeasurer : ITokenMeasurer
    {
        public ValueTask<int> CountTokensAsync(string text)
            => new(SharedTokenizer.CountTokensWithFactor(text, 1.24));

        public ValueTask<int> CountTokensAsync(MultimodalContent content)
        {
            ArgumentNullException.ThrowIfNull(content);

            int count = content switch
            {
                ImageContent image => CountClaudeImageTokens(image),
                PdfContent => throw new NotSupportedException("This model does not support this modality."),
                _ => throw new NotSupportedException("This model does not support this modality."),
            };

            return new ValueTask<int>(count);
        }

        private static int CountClaudeImageTokens(ImageContent image)
        {
            byte[]? bytes = image.Contents;
            if (bytes is null || bytes.Length == 0)
                return 0;

            if (ImageHeaderParser.TryGetSize(bytes, out int width, out int height))
            {
                (int resizedW, int resizedH) = MediaHelpers.ResizeToMaxSide(width, height, 1568);
                long pixels = (long)resizedW * resizedH;

                int approx = (int)Math.Ceiling(pixels / 9800d);
                return Math.Max(85, approx);
            }

            return Math.Max(85, bytes.Length / 15000);
        }
    }
}
