using System.Buffers.Binary;

namespace CountTokens.Internal
{
    internal static class ImageHeaderParser
    {
        public static bool TryGetSize(ReadOnlySpan<byte> contents, out int width, out int height)
        {
            width = 0;
            height = 0;

            if (contents.Length < 10)
                return false;

            if (TryGetPngSize(contents, out width, out height))
                return true;
            if (TryGetGifSize(contents, out width, out height))
                return true;
            if (TryGetBmpSize(contents, out width, out height))
                return true;
            if (TryGetWebpSize(contents, out width, out height))
                return true;
            if (TryGetJpegSize(contents, out width, out height))
                return true;

            width = 0;
            height = 0;
            return false;
        }

        private static bool TryGetPngSize(ReadOnlySpan<byte> contents, out int width, out int height)
        {
            width = 0;
            height = 0;

            if (contents.Length < 24)
                return false;

            if (contents[0] != 0x89 || contents[1] != 0x50 || contents[2] != 0x4E || contents[3] != 0x47 ||
                contents[4] != 0x0D || contents[5] != 0x0A || contents[6] != 0x1A || contents[7] != 0x0A)
                return false;

            width = (int)BinaryPrimitives.ReadUInt32BigEndian(contents.Slice(16, 4));
            height = (int)BinaryPrimitives.ReadUInt32BigEndian(contents.Slice(20, 4));

            return width > 0 && height > 0;
        }

        private static bool TryGetGifSize(ReadOnlySpan<byte> contents, out int width, out int height)
        {
            width = 0;
            height = 0;

            if (contents.Length < 10)
                return false;

            if (contents[0] != (byte)'G' || contents[1] != (byte)'I' || contents[2] != (byte)'F')
                return false;

            width = BinaryPrimitives.ReadUInt16LittleEndian(contents.Slice(6, 2));
            height = BinaryPrimitives.ReadUInt16LittleEndian(contents.Slice(8, 2));

            return width > 0 && height > 0;
        }

        private static bool TryGetBmpSize(ReadOnlySpan<byte> contents, out int width, out int height)
        {
            width = 0;
            height = 0;

            if (contents.Length < 26)
                return false;

            if (contents[0] != (byte)'B' || contents[1] != (byte)'M')
                return false;

            width = BinaryPrimitives.ReadInt32LittleEndian(contents.Slice(18, 4));
            height = BinaryPrimitives.ReadInt32LittleEndian(contents.Slice(22, 4));

            if (height < 0)
                height = -height;

            return width > 0 && height > 0;
        }

        private static bool TryGetWebpSize(ReadOnlySpan<byte> contents, out int width, out int height)
        {
            width = 0;
            height = 0;

            if (contents.Length < 30)
                return false;

            if (contents[0] != (byte)'R' || contents[1] != (byte)'I' || contents[2] != (byte)'F' || contents[3] != (byte)'F')
                return false;
            if (contents[8] != (byte)'W' || contents[9] != (byte)'E' || contents[10] != (byte)'B' || contents[11] != (byte)'P')
                return false;

            for (int i = 12; i + 8 <= contents.Length;)
            {
                if (i + 8 > contents.Length)
                    return false;

                uint chunkTag = BinaryPrimitives.ReadUInt32LittleEndian(contents.Slice(i, 4));
                int chunkSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(contents.Slice(i + 4, 4));
                int chunkDataOffset = i + 8;

                if (chunkDataOffset + chunkSize > contents.Length)
                    return false;

                const uint VP8X = ((uint)'V') | ((uint)'P' << 8) | ((uint)'8' << 16) | ((uint)'X' << 24);
                if (chunkTag == VP8X)
                {
                    if (chunkSize < 10)
                        return false;

                    int w = 1 + (contents[chunkDataOffset + 4] | (contents[chunkDataOffset + 5] << 8) | (contents[chunkDataOffset + 6] << 16));
                    int h = 1 + (contents[chunkDataOffset + 7] | (contents[chunkDataOffset + 8] << 8) | (contents[chunkDataOffset + 9] << 16));
                    if (w <= 0 || h <= 0)
                        return false;

                    width = w;
                    height = h;
                    return true;
                }

                int next = chunkDataOffset + chunkSize;
                if ((chunkSize & 1) == 1)
                    next++;
                i = next;
            }

            return false;
        }

        private static bool TryGetJpegSize(ReadOnlySpan<byte> contents, out int width, out int height)
        {
            width = 0;
            height = 0;

            if (contents.Length < 4)
                return false;

            if (contents[0] != 0xFF || contents[1] != 0xD8)
                return false;

            int index = 2;
            while (index + 4 <= contents.Length)
            {
                if (contents[index] != 0xFF)
                {
                    index++;
                    continue;
                }

                while (index < contents.Length && contents[index] == 0xFF)
                    index++;

                if (index >= contents.Length)
                    return false;

                byte marker = contents[index++];

                if (marker == 0xD9)
                    return false;

                if (marker == 0xDA)
                    return false;

                if (index + 2 > contents.Length)
                    return false;

                int segmentLength = BinaryPrimitives.ReadUInt16BigEndian(contents.Slice(index, 2));
                if (segmentLength < 2)
                    return false;

                if (index + segmentLength > contents.Length)
                    return false;

                if (IsStartOfFrameMarker(marker))
                {
                    if (segmentLength < 7)
                        return false;

                    height = BinaryPrimitives.ReadUInt16BigEndian(contents.Slice(index + 3, 2));
                    width = BinaryPrimitives.ReadUInt16BigEndian(contents.Slice(index + 5, 2));
                    return width > 0 && height > 0;
                }

                index += segmentLength;
            }

            return false;
        }

        private static bool IsStartOfFrameMarker(byte marker) => marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF;
    }
}
