using Diginsight.Logging;

namespace Diginsight.Diagnostics.TextWriting;

/// <summary>
/// Represents text-writer log metadata.
/// </summary>
public sealed class TextWriterLogMetadata : ILogMetadata
{
    /// <summary>
    /// Gets the callback that seals a line descriptor before writing.
    /// </summary>
    public Func<LineDescriptor, LineDescriptor>? SealLineDescriptor { get; set; }
}
