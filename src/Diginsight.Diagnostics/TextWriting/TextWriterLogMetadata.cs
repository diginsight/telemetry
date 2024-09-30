using Diginsight.Logging;

namespace Diginsight.Diagnostics.TextWriting;

public sealed class TextWriterLogMetadata : ILogMetadata
{
    public Func<LineDescriptor, LineDescriptor>? SealLineDescriptor { get; set; }
}
