namespace CountTokens.Content
{
    public abstract class MultimodalContent
    {
        public byte[] Contents { get; set; }

        public MultimodalContent()
        {
            Contents = Array.Empty<byte>();
        }
    }
}
