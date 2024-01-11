namespace Diginsight.Diagnostics.TextWriting;

public sealed class DeltaToken : ILineToken
{
    public static readonly ILineToken Instance = new DeltaToken();

    private DeltaToken() { }

    public void Apply(ref LineDescriptor lineDescriptor)
    {
        lineDescriptor.CustomAppenders.Add(DeltaAppender.Instance);
    }
}
