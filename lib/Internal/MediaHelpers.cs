namespace CountTokens.Internal
{
    internal static class MediaHelpers
    {
        public static (int width, int height) ResizeToMaxSide(int width, int height, int maxSide)
        {
            if (width <= 0 || height <= 0)
                return (width, height);

            int currentMax = Math.Max(width, height);
            if (currentMax <= maxSide)
                return (width, height);

            double scale = maxSide / (double)currentMax;
            int newWidth = Math.Max(1, (int)Math.Round(width * scale));
            int newHeight = Math.Max(1, (int)Math.Round(height * scale));
            return (newWidth, newHeight);
        }

        public static int CountPdfPages(ReadOnlySpan<byte> bytes)
        {
            ReadOnlySpan<byte> needle = "/Type"u8;
            ReadOnlySpan<byte> page = "/Page"u8;

            int pages = 0;

            for (int i = 0; i <= bytes.Length - needle.Length; i++)
            {
                if (!bytes.Slice(i, needle.Length).SequenceEqual(needle))
                    continue;

                int j = i + needle.Length;
                while (j < bytes.Length && (bytes[j] == (byte)' ' || bytes[j] == (byte)'\t' || bytes[j] == (byte)'\r' || bytes[j] == (byte)'\n'))
                    j++;

                if (j + page.Length > bytes.Length)
                    continue;

                if (bytes.Slice(j, page.Length).SequenceEqual(page))
                {
                    int k = j + page.Length;
                    if (k < bytes.Length && ((bytes[k] >= (byte)'A' && bytes[k] <= (byte)'Z') || (bytes[k] >= (byte)'a' && bytes[k] <= (byte)'z')))
                        continue;

                    pages++;
                }
            }

            return pages;
        }
    }
}
