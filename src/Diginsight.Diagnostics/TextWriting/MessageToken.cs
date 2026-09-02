namespace Diginsight.Diagnostics.TextWriting;

/// <summary>
/// Represents a line token that configures message placement and resizing.
/// </summary>
public sealed class MessageToken : ILineToken
{
    /// <summary>
    /// Gets the maximum message length.
    /// </summary>
    public int? MaxMessageLength { get; set; }

    /// <summary>
    /// Gets the maximum full line length.
    /// </summary>
    public int? MaxLineLength { get; set; }

    /// <inheritdoc />
    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.MaxMessageLength = MaxMessageLength;
        lineDescriptor.MaxLineLength = MaxLineLength;
    }

    /// <inheritdoc />
    public ILineToken Clone() => new MessageToken() { MaxMessageLength = MaxMessageLength, MaxLineLength = MaxLineLength };
}
