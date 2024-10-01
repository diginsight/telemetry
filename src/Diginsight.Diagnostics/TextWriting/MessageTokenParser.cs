namespace Diginsight.Diagnostics.TextWriting;

internal sealed class MessageTokenParser : ILineTokenParser
{
    public string TokenName => "message";

    public ILineToken Parse(ReadOnlySpan<char> tokenDetailSpan)
    {
        int? maxMessageLength;
        int? maxLineLength;

        if (tokenDetailSpan.IsEmpty)
        {
            maxMessageLength = null;
            maxLineLength = null;
        }
        else
        {
            if (tokenDetailSpan[0] != '|')
            {
                throw new FormatException("Expected '|' or nothing");
            }

            tokenDetailSpan = tokenDetailSpan[1..];
            int separatorIndex = tokenDetailSpan.IndexOf('|');
            if (separatorIndex < 0)
            {
#if NET || NETSTANDARD2_1_OR_GREATER
                // ReSharper disable once InlineTemporaryVariable
                ReadOnlySpan<char> src1 = tokenDetailSpan;
#else
                string src1 = tokenDetailSpan.ToString();
#endif
                maxMessageLength = int.TryParse(src1, out int tmp) ? tmp : throw new FormatException("Expected integer");
                maxLineLength = null;
            }
            else
            {
#if NET || NETSTANDARD2_1_OR_GREATER
                ReadOnlySpan<char> src1 = tokenDetailSpan[..separatorIndex];
#else
                string src1 = tokenDetailSpan[..separatorIndex].ToString();
#endif
                maxMessageLength = src1.Length == 0 ? null : int.TryParse(src1, out int tmp1) ? tmp1 : throw new FormatException("Expected integer");

#if NET || NETSTANDARD2_1_OR_GREATER
                ReadOnlySpan<char> src2 = tokenDetailSpan[(separatorIndex + 1)..];
#else
                string src2 = tokenDetailSpan[(separatorIndex + 1)..].ToString();
#endif
                maxLineLength = int.TryParse(src2, out int tmp2) ? tmp2 : throw new FormatException("Expected integer");
            }
        }

        return new MessageToken() { MaxMessageLength = maxMessageLength, MaxLineLength = maxLineLength };
    }
}
