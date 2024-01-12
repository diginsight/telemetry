namespace Diginsight.Diagnostics.TextWriting;

public sealed class IndentationToken : ILineToken
{
    public int? MaxDepth { get; set; }

    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.MaxIndentedDepth = MaxDepth ?? 10;
    }

    internal static ILineToken Parse(ReadOnlySpan<char> tokenSpan)
    {
        int? maxDepth;
        if (tokenSpan.IsEmpty)
        {
            maxDepth = null;
        }
        else if (tokenSpan[0] == '|')
        {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            ReadOnlySpan<char> src = tokenSpan[1..];
#else
            string src = tokenSpan[1..].ToString();
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
