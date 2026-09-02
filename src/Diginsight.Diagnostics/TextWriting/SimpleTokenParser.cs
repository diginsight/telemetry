namespace Diginsight.Diagnostics.TextWriting;

/// <summary>
/// Represents a line token parser for tokens without details.
/// </summary>
public sealed class SimpleTokenParser : ILineTokenParser
{
    private readonly ILineToken instance;

    /// <inheritdoc />
    public string TokenName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleTokenParser" /> class.
    /// </summary>
    /// <param name="tokenName">The token name recognized by the parser.</param>
    /// <param name="instance">The line token instance returned by the parser.</param>
    public SimpleTokenParser(string tokenName, ILineToken instance)
    {
        this.instance = instance;
        TokenName = tokenName;
    }

    /// <inheritdoc />
    public ILineToken Parse(ReadOnlySpan<char> tokenDetailSpan)
    {
        return tokenDetailSpan.IsEmpty ? instance : throw new FormatException("Expected nothing");
    }
}
