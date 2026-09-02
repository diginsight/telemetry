using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Diagnostics.TextWriting;

/// <summary>
/// Represents a mutable text-writing line descriptor while line tokens are applied.
/// </summary>
public ref struct MutableLineDescriptor
{
    [SuppressMessage("ReSharper", "ReplaceWithFieldKeyword")]
    private ICollection<IPrefixTokenAppender>? appenders;

    /// <summary>
    /// Gets the prefix token appenders.
    /// </summary>
    public ICollection<IPrefixTokenAppender> Appenders => appenders ??= new List<IPrefixTokenAppender>();

    /// <summary>
    /// Gets the maximum activity depth used for indentation.
    /// </summary>
    public int? MaxIndentedDepth { get; set; }

    /// <summary>
    /// Gets the maximum message length.
    /// </summary>
    public int? MaxMessageLength { get; set; }

    /// <summary>
    /// Gets the maximum full line length.
    /// </summary>
    public int? MaxLineLength { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MutableLineDescriptor" /> struct.
    /// </summary>
    /// <param name="descriptor">The line descriptor to copy.</param>
    public MutableLineDescriptor(LineDescriptor descriptor)
    {
        Appenders.AddRange(descriptor.Appenders);
        MaxIndentedDepth = descriptor.MaxIndentedDepth;
        MaxMessageLength = descriptor.MaxMessageLength;
        MaxLineLength = descriptor.MaxLineLength;
    }
}
