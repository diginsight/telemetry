namespace Diginsight.Diagnostics.TextWriting;

public sealed class DurationToken : ILineToken
{
    public static readonly ILineToken Instance = new DurationToken();

    private DurationToken() { }

    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.Appenders.Add(DurationAppender.Instance);
    }

    public ILineToken Clone() => this;
}
