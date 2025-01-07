namespace Diginsight.Diagnostics.TextWriting;

internal sealed class LogLevelTokenParser : ILineTokenParser
{
    public string TokenName => "loglevel";

    public ILineToken Parse(ReadOnlySpan<char> tokenDetailSpan)
    {
        int? length;
        if (tokenDetailSpan.IsEmpty)
        {
            length = null;
        }
        else if (tokenDetailSpan[0] == '|')
        {
#if NET || NETSTANDARD2_1_OR_GREATER
            ReadOnlySpan<char> src = tokenDetailSpan[1..];
#else
            string src = tokenDetailSpan[1..].ToString();
#endif
            length = int.TryParse(src, out int tmp) && tmp is >= 1 and <= 5
                ? tmp
                : throw new FormatException("Expected integer in the range 1-5");
        }
        else
        {
            throw new FormatException("Expected '|' or nothing");
        }

        return new LogLevelToken() { LengthUnsafe = length };
    }
}
