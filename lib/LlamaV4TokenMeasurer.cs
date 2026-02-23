using CountTokens.Content;
using CountTokens.Internal;

namespace CountTokens
{
    // Based on meta/llama-4-scout-17b-16e token counting logic (approximation).
    public sealed class LlamaV4TokenMeasurer : ITokenMeasurer
    {
        public ValueTask<int> CountTokensAsync(string text)
            => new(SharedTokenizer.CountTokensWithFactor(text, 2.0));

        public ValueTask<int> CountTokensAsync(MultimodalContent content)
        {
            ArgumentNullException.ThrowIfNull(content);

            int count = content switch
            {
                ImageContent image => CountLlamaImageTokens(image),
                _ => throw new NotSupportedException("This model does not support this modality."),
            };

            return new ValueTask<int>(count);
        }

        private static int CountLlamaImageTokens(ImageContent image)
        {
            byte[]? bytes = image.Contents;
            if (bytes is null || bytes.Length == 0)
                return 0;

            if (ImageHeaderParser.TryGetSize(bytes, out int width, out int height))
            {
                (int resizedW, int resizedH) = MediaHelpers.ResizeToMaxSide(width, height, 1568);
                long pixels = (long)resizedW * resizedH;

                int approx = (int)Math.Ceiling(pixels / 196d);
                return Math.Max(1500, approx);
            }

            return Math.Max(1500, bytes.Length / 500);
        }
    }
}
