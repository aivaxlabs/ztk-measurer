namespace CountTokens.Content
{
    public enum ImageDetailLevel
    {
        Auto = 0,
        Low = 1,
        High = 2,
    }

    public sealed class ImageContent : MultimodalContent
    {
        public string Extension { get; }
        public ImageDetailLevel Detail { get; }

        public ImageContent(byte[] contents, string extension, ImageDetailLevel detail = ImageDetailLevel.Auto)
        {
            ArgumentNullException.ThrowIfNull(contents);
            ArgumentException.ThrowIfNullOrWhiteSpace(extension);
            ArgumentOutOfRangeException.ThrowIfZero(contents.Length, "contents length");

            if (extension.StartsWith('.'))
                extension = extension[1..];

            Contents = contents;
            Extension = extension;
            Detail = detail;
        }
    }
}
