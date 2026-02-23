namespace CountTokens.Content
{
    public sealed class PdfContent : MultimodalContent
    {
        public string Extension => "pdf";

        public PdfContent(byte[] contents)
        {
            ArgumentNullException.ThrowIfNull(contents);
            ArgumentOutOfRangeException.ThrowIfZero(contents.Length, "contents length");
            Contents = contents;
        }
    }
}
