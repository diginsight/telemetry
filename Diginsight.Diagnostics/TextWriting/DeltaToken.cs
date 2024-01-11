namespace Diginsight.Diagnostics.TextWriting;

public sealed class DeltaToken : ILineToken
{
    public static readonly ILineToken Instance = new DeltaToken();

    private DeltaToken() { }

    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.Appenders.Add(DeltaAppender.Instance);
    }
}
