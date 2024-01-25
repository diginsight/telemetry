namespace Diginsight.Diagnostics.TextWriting;

public sealed class IndentationToken : ILineToken
{
    public int? MaxDepth { get; set; }

    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.MaxIndentedDepth = MaxDepth ?? 10;
    }

    public ILineToken Clone() => new IndentationToken() { MaxDepth = MaxDepth };
}
