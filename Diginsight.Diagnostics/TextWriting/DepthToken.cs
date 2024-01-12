namespace Diginsight.Diagnostics.TextWriting;

public sealed class DepthToken : ILineToken
{
    public static readonly ILineToken Instance = new DepthToken();

    private DepthToken() { }

    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.Appenders.Add(DepthAppender.Instance);
    }

    public ILineToken Clone() => this;
}
