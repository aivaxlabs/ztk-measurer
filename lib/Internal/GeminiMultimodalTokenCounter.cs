using CountTokens.Content;

namespace CountTokens.Internal
{
    internal static class GeminiMultimodalTokenCounter
    {
        public static int CountTokens(MultimodalContent content)
        {
            ArgumentNullException.ThrowIfNull(content);

            return content switch
            {
                ImageContent image => CountImageTokens(image),
                AudioContent audio => CountAudioTokens(audio),
                VideoContent video => CountVideoTokens(video),
                PdfContent pdf => CountPdfTokens(pdf),
                _ => CountFallbackTokens(content),
            };
        }

        public static int CountImageTokens(ImageContent image)
        {
            byte[]? bytes = image.Contents;
            if (bytes is null || bytes.Length == 0)
                return 0;

            // Empirically, gemini-2.5-flash-lite charges ~259 prompt tokens per image in this suite
            // regardless of resolution/detail.
            return 259;
        }

        public static int CountAudioTokens(AudioContent audio)
        {
            byte[]? bytes = audio.Contents;
            if (bytes is null || bytes.Length == 0)
                return 0;

            double seconds = AudioDurationEstimator.EstimateDurationSeconds(bytes, audio.Extension);
            if (seconds <= 0)
                return 10;

            int tokens = (int)Math.Ceiling(seconds * 32d);
            return Math.Clamp(tokens, 10, int.MaxValue);
        }

        public static int CountVideoTokens(VideoContent video)
        {
            byte[]? bytes = video.Contents;
            if (bytes is null || bytes.Length == 0)
                return 0;

            string ext = video.Extension.Trim().ToLowerInvariant();
            if (ext.Length > 0 && ext[0] == '.')
                ext = ext[1..];

            if (ext is "mp4" or "m4v" or "mov")
            {
                if (Mp4DurationReader.TryGetDurationSeconds(bytes, out double mp4Seconds) && mp4Seconds > 0)
                {
                    int durationTokens = (int)Math.Ceiling(mp4Seconds * 296d);
                    return Math.Clamp(durationTokens, 100, int.MaxValue);
                }
            }

            int bytesPerSecond = ext switch
            {
                "webm" => 187_500,
                "avi" => 312_500,
                "mov" => 312_500,
                "mkv" => 250_000,
                "mp4" => 250_000,
                "m4v" => 250_000,
                _ => 250_000,
            };

            double seconds = bytes.Length / (double)bytesPerSecond;
            if (seconds <= 0)
                return 100;

            int tokens = (int)Math.Ceiling(seconds * 296d);
            return Math.Clamp(tokens, 100, int.MaxValue);
        }

        public static int CountPdfTokens(PdfContent pdf)
        {
            byte[]? bytes = pdf.Contents;
            if (bytes is null || bytes.Length == 0)
                return 0;

            // Empirical approximation for gemini-2.5-flash-lite PDF token cost in this suite.
            // Page counting in PDFs is unreliable without a real parser; size-based buckets are
            // stable, allocation-free, and match the observed usage much better.
            int len = bytes.Length;

            if (len <= 600_000)
                return 258;

            if (len <= 2_500_000)
                return (int)Math.Ceiling(len / 764d);

            return (int)Math.Ceiling(len / 319d);
        }

        public static int CountFallbackTokens(MultimodalContent content)
        {
            byte[]? bytes = content.Contents;
            if (bytes is null || bytes.Length == 0)
                return 0;

            return Math.Max(10, bytes.Length / 4);
        }
    }
}
