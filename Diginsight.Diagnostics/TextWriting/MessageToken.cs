namespace Diginsight.Diagnostics.TextWriting;

public sealed class MessageToken : ILineToken
{
    public int? MaxMessageLength { get; set; }

    public int? MaxLineLength { get; set; }

    public void Apply(ref LineDescriptor lineDescriptor)
    {
        lineDescriptor.MaxMessageLength = MaxMessageLength;
        lineDescriptor.MaxLineLength = MaxLineLength;
    }
}
