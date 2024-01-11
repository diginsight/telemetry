namespace Diginsight.Diagnostics.TextWriting;

public sealed class MessageToken : ILineToken
{
    public int? MaxMessageLength { get; set; }
    public int? MaxLineLength { get; set; }

    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.MaxMessageLength = MaxMessageLength;
        lineDescriptor.MaxLineLength = MaxLineLength;
    }

    internal static ILineToken Parse(ReadOnlySpan<char> tokenSpan)
    {
        int? maxMessageLength;
        int? maxLineLength;

        if (tokenSpan.IsEmpty)
        {
            maxMessageLength = 0;
            maxLineLength = 0;
        }
        else
        {
            if (tokenSpan[0] != ';')
            {
                throw new FormatException("Expected ';' or nothing");
            }

            tokenSpan = tokenSpan[1..];
            int semicolonIndex = tokenSpan.IndexOf(';');
            if (semicolonIndex < 0)
            {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                ReadOnlySpan<char> src = tokenSpan;
#else
                string src = tokenSpan.ToString();
#endif
                maxMessageLength = int.TryParse(src, out int tmp) ? tmp : throw new FormatException("Expected integer");
                maxLineLength = 0;
            }
            else
            {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                ReadOnlySpan<char> src1 = tokenSpan[..semicolonIndex];
#else
                string src1 = tokenSpan[..semicolonIndex].ToString();
#endif
                maxMessageLength = src1.Length == 0 ? 0 : int.TryParse(src1, out int tmp1) ? tmp1 : throw new FormatException("Expected integer");

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                ReadOnlySpan<char> src2 = tokenSpan[(semicolonIndex + 1)..];
#else
                string src2 = tokenSpan[(semicolonIndex + 1)..].ToString();
#endif
                maxLineLength = int.TryParse(src2, out int tmp2) ? tmp2 : throw new FormatException("Expected integer");
            }
        }

        return new MessageToken() { MaxMessageLength = maxMessageLength, MaxLineLength = maxLineLength };
    }
}
