namespace CountTokens.Content
{
    public sealed class VideoContent : MultimodalContent
    {
        public string Extension { get; }

        public VideoContent(byte[] contents, string format)
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
