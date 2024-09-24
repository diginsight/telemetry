namespace Diginsight.Diagnostics.TextWriting;

public sealed class TextWriterLogMetadata : ILogMetadata
{
    public Func<int, int>? SealMaxMessageLength { get; set; }
}
