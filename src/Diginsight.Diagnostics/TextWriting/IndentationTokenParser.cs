namespace Diginsight.Diagnostics.TextWriting;

internal sealed class IndentationTokenParser : ILineTokenParser
{
    public string TokenName => "indentation";

    public ILineToken Parse(ReadOnlySpan<char> tokenDetailSpan)
    {
        int? maxDepth;
        if (tokenDetailSpan.IsEmpty)
        {
            maxDepth = null;
        }
        else if (tokenDetailSpan[0] == '|')
        {
#if NET || NETSTANDARD2_1_OR_GREATER
            ReadOnlySpan<char> src = tokenDetailSpan[1..];
#else
            string src = tokenDetailSpan[1..].ToString();
#endif
            maxDepth = int.TryParse(src, out int tmp) ? tmp : throw new FormatException("Expected integer");
        }
        else
        {
            throw new FormatException("Expected '|' or nothing");
        }

        return new IndentationToken() { MaxDepth = maxDepth };
    }
}
