namespace Diginsight.Diagnostics.TextWriting;

public sealed class TraceIdToken : ILineToken
{
    public static readonly ILineToken Instance = new TraceIdToken();

    private TraceIdToken() { }

    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.Appenders.Add(TraceIdAppender.Instance);
    }
}
