namespace Diginsight.Diagnostics.TextWriting;

public sealed class SimpleTokenParser : ILineTokenParser
{
    private readonly ILineToken instance;

    public string TokenName { get; }

    public SimpleTokenParser(string tokenName, ILineToken instance)
    {
        this.instance = instance;
        TokenName = tokenName;
    }

    public ILineToken Parse(ReadOnlySpan<char> tokenDetailSpan)
    {
        return tokenDetailSpan.IsEmpty ? instance : throw new FormatException("Expected nothing");
    }
}
