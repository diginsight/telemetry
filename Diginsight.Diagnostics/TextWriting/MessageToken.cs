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

    public ILineToken Clone() => new MessageToken() { MaxMessageLength = MaxMessageLength, MaxLineLength = MaxLineLength };

    internal static ILineToken Parse(ReadOnlySpan<char> tokenSpan)
    {
        int? maxMessageLength;
        int? maxLineLength;

        if (tokenSpan.IsEmpty)
        {
            maxMessageLength = null;
            maxLineLength = null;
        }
        else
        {
            if (tokenSpan[0] != '|')
            {
                throw new FormatException("Expected '|' or nothing");
            }

            tokenSpan = tokenSpan[1..];
            int separatorIndex = tokenSpan.IndexOf('|');
            if (separatorIndex < 0)
            {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                ReadOnlySpan<char> src = tokenSpan;
#else
                string src = tokenSpan.ToString();
#endif
                maxMessageLength = int.TryParse(src, out int tmp) ? tmp : throw new FormatException("Expected integer");
                maxLineLength = null;
            }
            else
            {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                ReadOnlySpan<char> src1 = tokenSpan[..separatorIndex];
#else
                string src1 = tokenSpan[..separatorIndex].ToString();
#endif
                maxMessageLength = src1.Length == 0 ? null : int.TryParse(src1, out int tmp1) ? tmp1 : throw new FormatException("Expected integer");

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                ReadOnlySpan<char> src2 = tokenSpan[(separatorIndex + 1)..];
#else
                string src2 = tokenSpan[(separatorIndex + 1)..].ToString();
#endif
                maxLineLength = int.TryParse(src2, out int tmp2) ? tmp2 : throw new FormatException("Expected integer");
            }
        }

        return new MessageToken() { MaxMessageLength = maxMessageLength, MaxLineLength = maxLineLength };
    }
}
