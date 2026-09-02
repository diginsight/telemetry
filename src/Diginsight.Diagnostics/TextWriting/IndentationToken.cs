namespace Diginsight.Diagnostics.TextWriting;

/// <summary>
/// Represents a line token that configures message indentation.
/// </summary>
public sealed class IndentationToken : ILineToken
{
    /// <summary>
    /// Gets the maximum activity depth used for indentation.
    /// </summary>
    public int? MaxDepth { get; set; }

    /// <inheritdoc />
    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.MaxIndentedDepth = MaxDepth ?? 10;
    }

    /// <inheritdoc />
    public ILineToken Clone() => new IndentationToken() { MaxDepth = MaxDepth };
}
