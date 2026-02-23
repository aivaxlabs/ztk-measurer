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

            if (ImageHeaderParser.TryGetSize(bytes, out int width, out int height))
            {
                int maxDim = Math.Max(width, height);
                ImageDetailLevel autoDetail = (maxDim > 2048) ? ImageDetailLevel.High : ImageDetailLevel.Low;

                ImageDetailLevel requested = image.Detail;
                ImageDetailLevel detail = requested switch
                {
                    ImageDetailLevel.Auto => autoDetail,
                    ImageDetailLevel.High => ImageDetailLevel.High,
                    ImageDetailLevel.Low => autoDetail == ImageDetailLevel.High ? ImageDetailLevel.High : ImageDetailLevel.Low,
                    _ => autoDetail,
                };

                (int resizedW, int resizedH) = MediaHelpers.ResizeToMaxSide(width, height, 1568);
                long pixels = (long)resizedW * resizedH;

                double divisor = detail == ImageDetailLevel.High ? 210d : 750d;
                int approx = (int)Math.Ceiling(pixels / divisor);
                if (approx < 85)
                    approx = 85;

                return approx;
            }

            int fallback = 85 + (bytes.Length / 1000);
            return Math.Max(85, fallback);
        }

        public static int CountAudioTokens(AudioContent audio)
        {
            byte[]? bytes = audio.Contents;
            if (bytes is null || bytes.Length == 0)
                return 0;

            double seconds = AudioDurationEstimator.EstimateDurationSeconds(bytes, audio.Extension);
            if (seconds <= 0)
                return 10;

            int tokens = (int)Math.Ceiling(seconds * 25.08d);
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
                    int durationTokens = (int)Math.Ceiling(mp4Seconds * 285d);
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

            int tokens = (int)Math.Ceiling(seconds * 285d);
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

            int pageBased = pages * 258;

            double bytesPerPage = bytes.Length / (double)pages;
            if (bytesPerPage > 300_000)
            {
                int bytesBased = (int)Math.Ceiling(bytes.Length / 292d);
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
