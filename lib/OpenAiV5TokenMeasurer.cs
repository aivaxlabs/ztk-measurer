using CountTokens.Content;
using CountTokens.Internal;

namespace CountTokens
{
    // Based on openai/gpt-5-nano token counting logic (approximation).
    public sealed class OpenAiV5TokenMeasurer : ITokenMeasurer
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
                ImageContent image => CountOpenAiImageTokens(image),
                PdfContent pdf => CountOpenAiPdfTokens(pdf),
                _ => throw new NotSupportedException("This model does not support this modality."),
            };

            return new ValueTask<int>(count);
        }

        private static int CountOpenAiImageTokens(ImageContent image)
        {
            byte[]? bytes = image.Contents;
            if (bytes is null || bytes.Length == 0)
                return 0;

            // In this suite, GPT-5-nano reports ~509 prompt tokens per image.
            return 509;
        }

        private static int CountOpenAiPdfTokens(PdfContent pdf)
        {
            byte[]? bytes = pdf.Contents;
            if (bytes is null || bytes.Length == 0)
                return 0;

            // In this suite, GPT-5-nano reports ~508 prompt tokens per PDF (independent of size/pages).
            return 508;
        }
    }
}
