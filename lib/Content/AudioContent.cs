namespace CountTokens.Content
{
    public sealed class AudioContent : MultimodalContent
    {
        public string Extension { get; }

        public AudioContent(byte[] contents, string format)
        {
            ArgumentNullException.ThrowIfNull(contents);
            ArgumentException.ThrowIfNullOrWhiteSpace(format);
            ArgumentOutOfRangeException.ThrowIfZero(contents.Length, "contents length");

            if (format.StartsWith('.'))
                format = format[1..];

            Contents = contents;
            Extension = format;
        }
    }
}
