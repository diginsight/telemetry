namespace Diginsight.Diagnostics.TextWriting;

public sealed class CategoryToken : ILineToken
{
    public int? Length { get; set; }

    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.Appenders.Add(new CategoryAppender(Length));
    }

    internal static ILineToken Parse(ReadOnlySpan<char> tokenSpan)
    {
        int? length;
        if (tokenSpan.IsEmpty)
        {
            length = null;
        }
        else if (tokenSpan[0] == ';')
        {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            ReadOnlySpan<char> src = tokenSpan[1..];
#else
            string src = tokenSpan[1..].ToString();
#endif
            length = int.TryParse(src, out int tmp) ? tmp : throw new FormatException("Expected integer");
        }
        else
        {
            throw new FormatException("Expected ';' or nothing");
        }

        return new CategoryToken() { Length = length };
    }
}
