using System.Buffers.Binary;

namespace CountTokens.Internal
{
    internal static class AudioDurationEstimator
    {
        public static double EstimateDurationSeconds(ReadOnlySpan<byte> contents, string extension)
        {
            if (contents.Length == 0)
                return 0;

            if (extension.Length > 0 && extension[0] == '.')
                extension = extension[1..];

            extension = extension.Trim().ToLowerInvariant();

            if (extension == "mp3" && TryGetMp3DurationSeconds(contents, out double mp3Seconds))
                return mp3Seconds;

            if (extension == "wav" && TryGetWavByteRate(contents, out int byteRate) && byteRate > 0)
            {
                int payloadBytes = contents.Length >= 44 ? (contents.Length - 44) : contents.Length;
                return payloadBytes / (double)byteRate;
            }

            int bytesPerSecond = extension switch
            {
                "mp3" => 16_000,
                "ogg" => 12_000,
                "opus" => 12_000,
                "aac" => 16_000,
                "m4a" => 16_000,
                "flac" => 100_000,
                "wma" => 16_000,
                _ => 16_000,
            };

            return contents.Length / (double)bytesPerSecond;
        }

        private static bool TryGetWavByteRate(ReadOnlySpan<byte> contents, out int byteRate)
        {
            byteRate = 0;

            if (contents.Length < 32)
                return false;

            if (contents[0] != (byte)'R' || contents[1] != (byte)'I' || contents[2] != (byte)'F' || contents[3] != (byte)'F')
                return false;
            if (contents[8] != (byte)'W' || contents[9] != (byte)'A' || contents[10] != (byte)'V' || contents[11] != (byte)'E')
                return false;

            byteRate = BinaryPrimitives.ReadInt32LittleEndian(contents.Slice(28, 4));
            return byteRate > 0;
        }

        private static bool TryGetMp3DurationSeconds(ReadOnlySpan<byte> contents, out double seconds)
        {
            seconds = 0;

            int offset = 0;
            if (TrySkipId3v2(contents, out int skipped))
                offset = skipped;

            if (!TryFindFirstMp3Frame(contents, offset, out int frameOffset))
                return false;

            if (!TryParseMp3FrameHeader(contents.Slice(frameOffset), out int bitrateKbps, out int sampleRateHz, out int samplesPerFrame, out int frameHeaderLen))
                return false;

            if (sampleRateHz <= 0 || samplesPerFrame <= 0)
                return false;

            if (TryGetMp3VbrFrameCount(contents, frameOffset, sampleRateHz, out long frames))
            {
                seconds = frames * (samplesPerFrame / (double)sampleRateHz);
                return seconds > 0;
            }

            if (bitrateKbps <= 0)
                return false;

            int audioBytes = contents.Length - frameOffset;
            if (audioBytes <= 0)
                return false;

            double bitrateBps = bitrateKbps * 1000d;
            seconds = (audioBytes * 8d) / bitrateBps;
            return seconds > 0;
        }

        private static bool TrySkipId3v2(ReadOnlySpan<byte> contents, out int skipped)
        {
            skipped = 0;
            if (contents.Length < 10)
                return false;

            if (contents[0] != (byte)'I' || contents[1] != (byte)'D' || contents[2] != (byte)'3')
                return false;

            int size = SyncSafeToInt(contents.Slice(6, 4));
            if (size < 0)
                return false;

            int total = 10 + size;
            if (total > contents.Length)
                total = contents.Length;

            skipped = total;
            return true;
        }

        private static int SyncSafeToInt(ReadOnlySpan<byte> b)
        {
            if (b.Length < 4)
                return -1;

            int v = (b[0] & 0x7F) << 21;
            v |= (b[1] & 0x7F) << 14;
            v |= (b[2] & 0x7F) << 7;
            v |= (b[3] & 0x7F);
            return v;
        }

        private static bool TryFindFirstMp3Frame(ReadOnlySpan<byte> contents, int start, out int frameOffset)
        {
            frameOffset = -1;
            if (start < 0)
                start = 0;

            int max = Math.Min(contents.Length - 4, start + 64_000);
            for (int i = start; i <= max; i++)
            {
                if (contents[i] != 0xFF)
                    continue;

                if ((contents[i + 1] & 0xE0) != 0xE0)
                    continue;

                if (TryParseMp3FrameHeader(contents.Slice(i), out _, out _, out _, out _))
                {
                    frameOffset = i;
                    return true;
                }
            }

            return false;
        }

        private static bool TryParseMp3FrameHeader(ReadOnlySpan<byte> frame, out int bitrateKbps, out int sampleRateHz, out int samplesPerFrame, out int headerLen)
        {
            bitrateKbps = 0;
            sampleRateHz = 0;
            samplesPerFrame = 0;
            headerLen = 4;

            if (frame.Length < 4)
                return false;

            uint h = BinaryPrimitives.ReadUInt32BigEndian(frame.Slice(0, 4));
            if ((h & 0xFFE00000u) != 0xFFE00000u)
                return false;

            int versionBits = (int)((h >> 19) & 0x3);
            int layerBits = (int)((h >> 17) & 0x3);
            int bitrateIndex = (int)((h >> 12) & 0xF);
            int sampleRateIndex = (int)((h >> 10) & 0x3);
            int paddingBit = (int)((h >> 9) & 0x1);
            int channelMode = (int)((h >> 6) & 0x3);

            if (versionBits == 1 || layerBits == 0 || bitrateIndex == 0 || bitrateIndex == 15 || sampleRateIndex == 3)
                return false;

            bool mpeg1 = versionBits == 3;
            bool mpeg2 = versionBits == 2;
            bool mpeg25 = versionBits == 0;

            int layer = layerBits switch
            {
                3 => 1,
                2 => 2,
                1 => 3,
                _ => 0
            };
            if (layer == 0)
                return false;

            int baseSampleRate = sampleRateIndex switch
            {
                0 => 44100,
                1 => 48000,
                2 => 32000,
                _ => 0
            };
            if (baseSampleRate == 0)
                return false;

            if (mpeg2)
                baseSampleRate /= 2;
            else if (mpeg25)
                baseSampleRate /= 4;

            sampleRateHz = baseSampleRate;

            int[,] bitrateTable = mpeg1
                ? new int[,]
                {
                    { 0, 32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448, 0 },
                    { 0, 32, 48, 56,  64,  80,  96, 112, 128, 160, 192, 224, 256, 320, 384, 0 },
                    { 0, 32, 40, 48,  56,  64,  80,  96, 112, 128, 160, 192, 224, 256, 320, 0 },
                }
                : new int[,]
                {
                    { 0, 32, 48, 56,  64,  80,  96, 112, 128, 144, 160, 176, 192, 224, 256, 0 },
                    { 0,  8, 16, 24,  32,  40,  48,  56,  64,  80,  96, 112, 128, 144, 160, 0 },
                    { 0,  8, 16, 24,  32,  40,  48,  56,  64,  80,  96, 112, 128, 144, 160, 0 },
                };

            int layerRow = layer switch
            {
                1 => 0,
                2 => 1,
                3 => 2,
                _ => 2
            };

            bitrateKbps = bitrateTable[layerRow, bitrateIndex];

            samplesPerFrame = layer switch
            {
                1 => 384,
                2 => 1152,
                3 when mpeg1 => 1152,
                3 => 576,
                _ => 1152
            };

            bool hasCrc = ((h >> 16) & 0x1) == 0;
            headerLen = hasCrc ? 6 : 4;

            _ = paddingBit;
            _ = channelMode;

            return bitrateKbps > 0 && sampleRateHz > 0;
        }

        private static bool TryGetMp3VbrFrameCount(ReadOnlySpan<byte> contents, int frameOffset, int sampleRateHz, out long frames)
        {
            frames = 0;
            if (contents.Length < frameOffset + 200)
                return false;

            ReadOnlySpan<byte> frame = contents.Slice(frameOffset);
            if (!TryParseMp3FrameHeader(frame, out _, out _, out _, out int headerLen))
                return false;

            int sideInfoLen;
            {
                uint h = BinaryPrimitives.ReadUInt32BigEndian(frame.Slice(0, 4));
                int versionBits = (int)((h >> 19) & 0x3);
                int channelMode = (int)((h >> 6) & 0x3);
                bool mpeg1 = versionBits == 3;
                bool mono = channelMode == 3;
                sideInfoLen = mpeg1 ? (mono ? 17 : 32) : (mono ? 9 : 17);
            }

            int xingOffset = headerLen + sideInfoLen;
            if (xingOffset + 16 > frame.Length)
                return false;

            bool isXing = frame.Slice(xingOffset, 4).SequenceEqual("Xing"u8) || frame.Slice(xingOffset, 4).SequenceEqual("Info"u8);
            if (isXing)
            {
                uint flags = BinaryPrimitives.ReadUInt32BigEndian(frame.Slice(xingOffset + 4, 4));
                bool hasFrames = (flags & 0x1) != 0;
                if (!hasFrames)
                    return false;

                frames = BinaryPrimitives.ReadUInt32BigEndian(frame.Slice(xingOffset + 8, 4));
                return frames > 0;
            }

            int vbriOffset = headerLen + 32;
            if (vbriOffset + 26 <= frame.Length && frame.Slice(vbriOffset, 4).SequenceEqual("VBRI"u8))
            {
                frames = BinaryPrimitives.ReadUInt32BigEndian(frame.Slice(vbriOffset + 14, 4));
                return frames > 0;
            }

            _ = sampleRateHz;
            return false;
        }
    }
}
