using System.Buffers.Binary;

namespace CountTokens.Internal
{
    internal static class Mp4DurationReader
    {
        public static bool TryGetDurationSeconds(ReadOnlySpan<byte> contents, out double seconds)
        {
            seconds = 0;

            if (contents.Length < 32)
                return false;

            int moovOffset = FindAtom(contents, "moov", 0, contents.Length);
            if (moovOffset < 0)
                return false;

            if (!TryReadAtomSize(contents, moovOffset, out long moovSize, out int moovHeaderSize))
                return false;

            long moovEnd = moovOffset + moovSize;
            if (moovEnd > contents.Length)
                moovEnd = contents.Length;

            if (TryGetMvhdDurationSeconds(contents, moovOffset + moovHeaderSize, (int)moovEnd, out seconds) && seconds > 0)
                return true;

            if (TryGetFragmentedDurationSeconds(contents, moovOffset, (int)moovEnd, out seconds) && seconds > 0)
                return true;

            seconds = 0;
            return false;
        }

        private static bool TryGetMvhdDurationSeconds(ReadOnlySpan<byte> contents, int moovPayloadStart, int moovEnd, out double seconds)
        {
            seconds = 0;

            int mvhdOffset = FindAtom(contents, "mvhd", moovPayloadStart, moovEnd);
            if (mvhdOffset < 0)
                return false;

            if (!TryReadAtomSize(contents, mvhdOffset, out long mvhdSize, out int mvhdHeaderSize))
                return false;

            int payloadOffset = mvhdOffset + mvhdHeaderSize;
            int payloadLen = (int)Math.Min(mvhdSize - mvhdHeaderSize, contents.Length - payloadOffset);
            if (payloadLen < 20)
                return false;

            byte version = contents[payloadOffset];

            if (version == 0)
            {
                uint timescale = BinaryPrimitives.ReadUInt32BigEndian(contents.Slice(payloadOffset + 12, 4));
                uint duration = BinaryPrimitives.ReadUInt32BigEndian(contents.Slice(payloadOffset + 16, 4));
                if (timescale == 0 || duration == 0)
                    return false;

                seconds = duration / (double)timescale;
                return seconds > 0;
            }

            if (version == 1)
            {
                if (payloadLen < 32)
                    return false;

                uint timescale = BinaryPrimitives.ReadUInt32BigEndian(contents.Slice(payloadOffset + 20, 4));
                ulong duration = BinaryPrimitives.ReadUInt64BigEndian(contents.Slice(payloadOffset + 24, 8));
                if (timescale == 0 || duration == 0)
                    return false;

                seconds = duration / (double)timescale;
                return seconds > 0;
            }

            return false;
        }

        private static bool TryGetFragmentedDurationSeconds(ReadOnlySpan<byte> contents, int moovOffset, int moovEnd, out double seconds)
        {
            seconds = 0;

            ReadOnlySpan<byte> moov = contents.Slice(moovOffset, moovEnd - moovOffset);
            if (!TryGetTrackTimescales(moov, out Dictionary<int, uint> trackTimescales))
                return false;

            double maxSeconds = 0;

            for (int offset = 0; offset + 8 <= contents.Length;)
            {
                if (!TryReadAtomSize(contents, offset, out long size, out int headerSize))
                    break;

                if (size < headerSize)
                    break;

                if (offset + 8 <= contents.Length)
                {
                    ReadOnlySpan<byte> type = contents.Slice(offset + 4, 4);
                    if (type.SequenceEqual("moof"u8))
                    {
                        int moofLen = (int)Math.Min(size, contents.Length - offset);
                        if (TryAccumulateMoofDurationSeconds(contents.Slice(offset, moofLen), trackTimescales, ref maxSeconds))
                        {
                        }
                    }
                }

                long next = offset + size;
                if (next <= offset)
                    break;

                offset = (int)Math.Min(next, int.MaxValue);
            }

            if (maxSeconds <= 0)
                return false;

            seconds = maxSeconds;
            return true;
        }

        private static bool TryGetTrackTimescales(ReadOnlySpan<byte> moov, out Dictionary<int, uint> timescales)
        {
            timescales = new Dictionary<int, uint>();

            if (!TryReadAtomSize(moov, 0, out long moovSize, out int moovHeaderSize))
                return false;

            int end = (int)Math.Min(moovSize, moov.Length);
            for (int offset = moovHeaderSize; offset + 8 <= end;)
            {
                if (!TryReadAtomSize(moov, offset, out long size, out int headerSize))
                    break;

                if (size < headerSize)
                    break;

                ReadOnlySpan<byte> type = moov.Slice(offset + 4, 4);
                if (type.SequenceEqual("trak"u8))
                {
                    int trakLen = (int)Math.Min(size, end - offset);
                    ReadOnlySpan<byte> trak = moov.Slice(offset, trakLen);
                    if (TryGetTrackIdAndTimescale(trak, out int trackId, out uint timescale) && trackId > 0 && timescale > 0)
                        timescales[trackId] = timescale;
                }

                offset += (int)size;
            }

            return timescales.Count > 0;
        }

        private static bool TryGetTrackIdAndTimescale(ReadOnlySpan<byte> trak, out int trackId, out uint timescale)
        {
            trackId = 0;
            timescale = 0;

            if (!TryReadAtomSize(trak, 0, out long trakSize, out int trakHeaderSize))
                return false;

            int end = (int)Math.Min(trakSize, trak.Length);

            int tkhdOffset = FindAtom(trak, "tkhd", trakHeaderSize, end);
            if (tkhdOffset >= 0 && TryReadTrackIdFromTkhd(trak, tkhdOffset, out trackId))
            {
            }

            int mdiaOffset = FindAtom(trak, "mdia", trakHeaderSize, end);
            if (mdiaOffset < 0)
                return false;

            if (!TryReadAtomSize(trak, mdiaOffset, out long mdiaSize, out int mdiaHeaderSize))
                return false;

            int mdiaEnd = mdiaOffset + (int)Math.Min(mdiaSize, trak.Length - mdiaOffset);
            int mdhdOffset = FindAtom(trak, "mdhd", mdiaOffset + mdiaHeaderSize, mdiaEnd);
            if (mdhdOffset < 0)
                return false;

            if (!TryReadTimescaleFromMdhd(trak, mdhdOffset, out timescale))
                return false;

            return trackId > 0 && timescale > 0;
        }

        private static bool TryReadTrackIdFromTkhd(ReadOnlySpan<byte> trak, int tkhdOffset, out int trackId)
        {
            trackId = 0;

            if (!TryReadAtomSize(trak, tkhdOffset, out long tkhdSize, out int tkhdHeaderSize))
                return false;

            int payloadOffset = tkhdOffset + tkhdHeaderSize;
            int payloadLen = (int)Math.Min(tkhdSize - tkhdHeaderSize, trak.Length - payloadOffset);
            if (payloadLen < 24)
                return false;

            byte version = trak[payloadOffset];
            if (version == 0)
            {
                trackId = (int)BinaryPrimitives.ReadUInt32BigEndian(trak.Slice(payloadOffset + 12, 4));
                return trackId > 0;
            }

            if (version == 1)
            {
                if (payloadLen < 36)
                    return false;

                trackId = (int)BinaryPrimitives.ReadUInt32BigEndian(trak.Slice(payloadOffset + 20, 4));
                return trackId > 0;
            }

            return false;
        }

        private static bool TryReadTimescaleFromMdhd(ReadOnlySpan<byte> trak, int mdhdOffset, out uint timescale)
        {
            timescale = 0;

            if (!TryReadAtomSize(trak, mdhdOffset, out long mdhdSize, out int mdhdHeaderSize))
                return false;

            int payloadOffset = mdhdOffset + mdhdHeaderSize;
            int payloadLen = (int)Math.Min(mdhdSize - mdhdHeaderSize, trak.Length - payloadOffset);
            if (payloadLen < 20)
                return false;

            byte version = trak[payloadOffset];
            if (version == 0)
            {
                timescale = BinaryPrimitives.ReadUInt32BigEndian(trak.Slice(payloadOffset + 12, 4));
                return timescale > 0;
            }

            if (version == 1)
            {
                if (payloadLen < 32)
                    return false;

                timescale = BinaryPrimitives.ReadUInt32BigEndian(trak.Slice(payloadOffset + 20, 4));
                return timescale > 0;
            }

            return false;
        }

        private static bool TryAccumulateMoofDurationSeconds(ReadOnlySpan<byte> moof, Dictionary<int, uint> trackTimescales, ref double maxSeconds)
        {
            if (!TryReadAtomSize(moof, 0, out long moofSize, out int moofHeaderSize))
                return false;

            int end = (int)Math.Min(moofSize, moof.Length);

            for (int offset = moofHeaderSize; offset + 8 <= end;)
            {
                if (!TryReadAtomSize(moof, offset, out long size, out int headerSize))
                    break;

                if (size < headerSize)
                    break;

                ReadOnlySpan<byte> type = moof.Slice(offset + 4, 4);
                if (type.SequenceEqual("traf"u8))
                {
                    int trafLen = (int)Math.Min(size, end - offset);
                    ReadOnlySpan<byte> traf = moof.Slice(offset, trafLen);
                    if (TryGetTrafEndTime(traf, out int trackId, out ulong endTime))
                    {
                        if (trackTimescales.TryGetValue(trackId, out uint timescale) && timescale > 0)
                        {
                            double sec = endTime / (double)timescale;
                            if (sec > maxSeconds)
                                maxSeconds = sec;
                        }
                    }
                }

                offset += (int)size;
            }

            return true;
        }

        private static bool TryGetTrafEndTime(ReadOnlySpan<byte> traf, out int trackId, out ulong endTime)
        {
            trackId = 0;
            endTime = 0;

            if (!TryReadAtomSize(traf, 0, out long trafSize, out int trafHeaderSize))
                return false;

            int end = (int)Math.Min(trafSize, traf.Length);

            int tfhdOffset = FindAtom(traf, "tfhd", trafHeaderSize, end);
            int tfdtOffset = FindAtom(traf, "tfdt", trafHeaderSize, end);
            int trunOffset = FindAtom(traf, "trun", trafHeaderSize, end);
            if (tfhdOffset < 0 || tfdtOffset < 0 || trunOffset < 0)
                return false;

            if (!TryParseTfhd(traf, tfhdOffset, out trackId, out uint defaultSampleDuration))
                return false;

            if (!TryParseTfdt(traf, tfdtOffset, out ulong baseDecodeTime))
                return false;

            if (!TryParseTrunDuration(traf, trunOffset, defaultSampleDuration, out ulong duration))
                return false;

            endTime = baseDecodeTime + duration;
            return endTime > 0;
        }

        private static bool TryParseTfhd(ReadOnlySpan<byte> traf, int tfhdOffset, out int trackId, out uint defaultSampleDuration)
        {
            trackId = 0;
            defaultSampleDuration = 0;

            if (!TryReadAtomSize(traf, tfhdOffset, out long tfhdSize, out int tfhdHeaderSize))
                return false;

            int payloadOffset = tfhdOffset + tfhdHeaderSize;
            int payloadLen = (int)Math.Min(tfhdSize - tfhdHeaderSize, traf.Length - payloadOffset);
            if (payloadLen < 8)
                return false;

            if (!TryReadFullBoxVersionFlags(traf.Slice(payloadOffset, payloadLen), out _, out int flags))
                return false;

            int cursor = payloadOffset + 4;
            if (cursor + 4 > traf.Length)
                return false;

            trackId = (int)BinaryPrimitives.ReadUInt32BigEndian(traf.Slice(cursor, 4));
            cursor += 4;

            if ((flags & 0x000001) != 0)
                cursor += 8;
            if ((flags & 0x000002) != 0)
                cursor += 4;
            if ((flags & 0x000008) != 0)
            {
                if (cursor + 4 > traf.Length)
                    return false;

                defaultSampleDuration = BinaryPrimitives.ReadUInt32BigEndian(traf.Slice(cursor, 4));
                cursor += 4;
            }

            return trackId > 0;
        }

        private static bool TryParseTfdt(ReadOnlySpan<byte> traf, int tfdtOffset, out ulong baseDecodeTime)
        {
            baseDecodeTime = 0;

            if (!TryReadAtomSize(traf, tfdtOffset, out long tfdtSize, out int tfdtHeaderSize))
                return false;

            int payloadOffset = tfdtOffset + tfdtHeaderSize;
            int payloadLen = (int)Math.Min(tfdtSize - tfdtHeaderSize, traf.Length - payloadOffset);
            if (payloadLen < 8)
                return false;

            if (!TryReadFullBoxVersionFlags(traf.Slice(payloadOffset, payloadLen), out byte version, out _))
                return false;

            int cursor = payloadOffset + 4;

            if (version == 0)
            {
                if (cursor + 4 > traf.Length)
                    return false;

                baseDecodeTime = BinaryPrimitives.ReadUInt32BigEndian(traf.Slice(cursor, 4));
                return true;
            }

            if (version == 1)
            {
                if (cursor + 8 > traf.Length)
                    return false;

                baseDecodeTime = BinaryPrimitives.ReadUInt64BigEndian(traf.Slice(cursor, 8));
                return true;
            }

            return false;
        }

        private static bool TryParseTrunDuration(ReadOnlySpan<byte> traf, int trunOffset, uint defaultSampleDuration, out ulong duration)
        {
            duration = 0;

            if (!TryReadAtomSize(traf, trunOffset, out long trunSize, out int trunHeaderSize))
                return false;

            int payloadOffset = trunOffset + trunHeaderSize;
            int payloadLen = (int)Math.Min(trunSize - trunHeaderSize, traf.Length - payloadOffset);
            if (payloadLen < 12)
                return false;

            if (!TryReadFullBoxVersionFlags(traf.Slice(payloadOffset, payloadLen), out _, out int flags))
                return false;

            int cursor = payloadOffset + 4;

            if (cursor + 4 > traf.Length)
                return false;

            uint sampleCount = BinaryPrimitives.ReadUInt32BigEndian(traf.Slice(cursor, 4));
            cursor += 4;

            if ((flags & 0x000001) != 0)
                cursor += 4;
            if ((flags & 0x000004) != 0)
                cursor += 4;

            bool hasDuration = (flags & 0x000100) != 0;
            bool hasSize = (flags & 0x000200) != 0;
            bool hasFlags = (flags & 0x000400) != 0;
            bool hasCto = (flags & 0x000800) != 0;

            if (!hasDuration)
            {
                if (defaultSampleDuration == 0)
                    return false;

                duration = (ulong)sampleCount * defaultSampleDuration;
                return duration > 0;
            }

            int perSample = (hasDuration ? 4 : 0) + (hasSize ? 4 : 0) + (hasFlags ? 4 : 0) + (hasCto ? 4 : 0);
            if (perSample <= 0)
                return false;

            ulong sum = 0;
            for (uint i = 0; i < sampleCount; i++)
            {
                if (cursor + perSample > traf.Length)
                    return false;

                sum += BinaryPrimitives.ReadUInt32BigEndian(traf.Slice(cursor, 4));
                cursor += perSample;
            }

            duration = sum;
            return duration > 0;
        }

        private static bool TryReadFullBoxVersionFlags(ReadOnlySpan<byte> payload, out byte version, out int flags)
        {
            version = 0;
            flags = 0;

            if (payload.Length < 4)
                return false;

            version = payload[0];
            flags = (payload[1] << 16) | (payload[2] << 8) | payload[3];
            return true;
        }

        private static bool TryReadAtomSize(ReadOnlySpan<byte> contents, int offset, out long size, out int headerSize)
        {
            size = 0;
            headerSize = 0;

            if (offset < 0 || offset + 8 > contents.Length)
                return false;

            uint size32 = BinaryPrimitives.ReadUInt32BigEndian(contents.Slice(offset, 4));
            headerSize = 8;

            if (size32 == 0)
            {
                size = contents.Length - offset;
                return size >= 8;
            }

            if (size32 == 1)
            {
                if (offset + 16 > contents.Length)
                    return false;

                ulong size64 = BinaryPrimitives.ReadUInt64BigEndian(contents.Slice(offset + 8, 8));
                headerSize = 16;
                size = (long)size64;
                return size >= 16;
            }

            size = size32;
            return size >= 8;
        }

        private static int FindAtom(ReadOnlySpan<byte> contents, string atom, int start, int end)
        {
            if (end > contents.Length)
                end = contents.Length;
            if (start < 0)
                start = 0;
            if (end - start < 8)
                return -1;

            for (int i = start; i + 8 <= end;)
            {
                if (!TryReadAtomSize(contents, i, out long size, out int headerSize))
                    return -1;

                if (i + 8 <= contents.Length)
                {
                    ReadOnlySpan<byte> type = contents.Slice(i + 4, 4);
                    if (type.Length == 4 && type[0] == atom[0] && type[1] == atom[1] && type[2] == atom[2] && type[3] == atom[3])
                        return i;
                }

                if (size <= 0)
                    return -1;

                long next = i + size;
                if (next <= i)
                    return -1;

                i = (int)Math.Min(next, int.MaxValue);
            }

            return -1;
        }
    }
}
