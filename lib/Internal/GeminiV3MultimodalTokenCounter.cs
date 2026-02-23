using CountTokens.Content;

namespace CountTokens.Internal
{
    internal static class GeminiV3MultimodalTokenCounter
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

            if (ImageHeaderParser.TryGetSize(bytes, out int width, out int height))
            {
                (int resizedW, int resizedH) = MediaHelpers.ResizeToMaxSide(width, height, 1568);
                long pixels = (long)resizedW * resizedH;

                int approx = (int)Math.Ceiling(pixels / 720d);
                return Math.Max(500, approx);
            }

            return Math.Max(500, 500 + (bytes.Length / 5000));
        }

        public static int CountAudioTokens(AudioContent audio)
        {
            byte[]? bytes = audio.Contents;
            if (bytes is null || bytes.Length == 0)
                return 0;

            double seconds = AudioDurationEstimator.EstimateDurationSeconds(bytes, audio.Extension);
            if (seconds <= 0)
                return 10;

            int tokens = (int)Math.Ceiling(seconds * 25d);
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
                    int durationTokens = (int)Math.Ceiling(mp4Seconds * 88.5d);
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

            int tokens = (int)Math.Ceiling(seconds * 88.5d);
            return Math.Clamp(tokens, 100, int.MaxValue);
        }

        public static int CountPdfTokens(PdfContent pdf)
        {
            byte[]? bytes = pdf.Contents;
            if (bytes is null || bytes.Length == 0)
                return 0;

            int pages = MediaHelpers.CountPdfPages(bytes);
            if (pages <= 0)
                pages = 1;

            int pageBased = pages * 260;

            double bytesPerPage = bytes.Length / (double)pages;
            if (bytesPerPage > 80_000)
            {
                int bytesBased = (int)Math.Ceiling(bytes.Length / 224d);
                return Math.Max(pageBased, bytesBased);
            }

            return Math.Max(100, pageBased);
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
