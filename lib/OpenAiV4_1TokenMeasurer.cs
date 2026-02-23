using CountTokens.Content;
using CountTokens.Internal;

namespace CountTokens
{
    // Based on openai/gpt-4.1-nano token counting logic (approximation).
    public sealed class OpenAiV4_1TokenMeasurer : ITokenMeasurer
    {
        public ValueTask<int> CountTokensAsync(string text)
        {
            int baseCount = SharedTokenizer.CountTokens(text);
            if (baseCount <= 0)
                return new ValueTask<int>(0);

            // Use overhead for short texts, converges to exact for longer texts
            int overhead = baseCount < 200 ? 7 : (int)Math.Ceiling((200.0 - baseCount) * 7.0 / 200.0);
            overhead = Math.Max(0, overhead);
            return new ValueTask<int>(baseCount + overhead);
        }

        public ValueTask<int> CountTokensAsync(MultimodalContent content)
        {
            ArgumentNullException.ThrowIfNull(content);

            int count = content switch
            {
                ImageContent image => CountOpenAi41ImageTokens(image),
                _ => throw new NotSupportedException("This model does not support this modality."),
            };

            return new ValueTask<int>(count);
        }

        private static int CountOpenAi41ImageTokens(ImageContent image)
        {
            byte[]? bytes = image.Contents;
            if (bytes is null || bytes.Length == 0)
                return 0;

            if (ImageHeaderParser.TryGetSize(bytes, out int width, out int height))
            {
                (int resizedW, int resizedH) = MediaHelpers.ResizeToMaxSide(width, height, 2048);
                long pixels = (long)resizedW * resizedH;

                int approx = (int)Math.Ceiling(pixels / 10000d);
                return Math.Max(85, approx);
            }

            return Math.Max(85, 85 + (bytes.Length / 80000));
        }
    }
}
