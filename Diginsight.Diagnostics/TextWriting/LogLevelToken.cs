namespace Diginsight.Diagnostics.TextWriting;

public sealed class LogLevelToken : ILineToken
{
    private int? length;

    public int? Length
    {
        get => length;
        set => length = value is < 1 or > 5 ? throw new ArgumentOutOfRangeException(nameof(Length), "Must be null or in the range 1-5") : value;
    }

    public void Apply(ref LineDescriptor lineDescriptor)
    {
        lineDescriptor.CustomAppenders.Add(LogLevelAppender.UnsafeFor(length));
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
            length = int.TryParse(src, out int tmp) && tmp is >= 1 and <= 5
                ? tmp
                : throw new FormatException("Expected integer in the range 1-5");
        }
        else
        {
            throw new FormatException("Expected ';' or nothing");
        }

        return new LogLevelToken() { length = length };
    }
}
