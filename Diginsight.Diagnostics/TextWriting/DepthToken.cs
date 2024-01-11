namespace Diginsight.Diagnostics.TextWriting;

public sealed class DepthToken : ILineToken
{
    public int? MaxIndented { get; set; }

    public void Apply(ref LineDescriptor lineDescriptor)
    {
        lineDescriptor.CustomAppenders.Add(DepthAppender.Instance);
        lineDescriptor.MaxIndentedDepth = MaxIndented;
    }

    internal static ILineToken Parse(ReadOnlySpan<char> tokenSpan)
    {
        int? maxIndented;
        if (tokenSpan.IsEmpty)
        {
            maxIndented = null;
        }
        else if (tokenSpan[0] == ';')
        {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            ReadOnlySpan<char> src = tokenSpan[1..];
#else
            string src = tokenSpan[1..].ToString();
#endif
            maxIndented = int.TryParse(src, out int tmp) ? tmp : throw new FormatException("Expected integer");
        }
        else
        {
            throw new FormatException("Expected ';' or nothing");
        }

        return new DepthToken() { MaxIndented = maxIndented };
    }
}
