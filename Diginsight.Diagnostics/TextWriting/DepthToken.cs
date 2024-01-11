namespace Diginsight.Diagnostics.TextWriting;

public sealed class DepthToken : ILineToken
{
    public int? MaxIndented { get; set; }

    public void Apply(ref LineDescriptor lineDescriptor)
    {
        lineDescriptor.CustomAppenders.Add(DepthAppender.Instance);
        lineDescriptor.MaxIndentedDepth = MaxIndented;
    }
}
