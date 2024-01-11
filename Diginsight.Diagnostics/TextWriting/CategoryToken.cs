namespace Diginsight.Diagnostics.TextWriting;

public sealed class CategoryToken : ILineToken
{
    public int? Length { get; set; }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    // TODO Promote to netstandard2.0
    internal static CategoryToken Parse(ReadOnlySpan<char> tokenSpan)
    {
        int? length;
        if (tokenSpan.IsEmpty)
        {
            length = null;
        }
        else if (tokenSpan[0] == ';')
        {
            length = int.TryParse(tokenSpan[1..], out int cl) ? cl : throw new FormatException("Expected integer");
        }
        else
        {
            throw new FormatException("Expected ';' or nothing");
        }

        return new CategoryToken() { Length = length };
    }
#endif

    public void Apply(ref LineDescriptor lineDescriptor)
    {
        lineDescriptor.CustomAppenders.Add(new CategoryAppender(Length));
    }
}
