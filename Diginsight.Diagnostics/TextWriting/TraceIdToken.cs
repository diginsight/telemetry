namespace Diginsight.Diagnostics.TextWriting;

public sealed class TraceIdToken : ILineToken
{
    public static readonly ILineToken Instance = new TraceIdToken();

    private TraceIdToken() { }

    public void Apply(ref LineDescriptor lineDescriptor)
    {
        lineDescriptor.CustomAppenders.Add(TraceIdAppender.Instance);
    }
}
