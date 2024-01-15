namespace Diginsight.Diagnostics.TextWriting;

public sealed class LogLevelToken : ILineToken
{
    private int? length;

    public int? Length
    {
        get => length;
        set => length = value is < 1 or > 5 ? throw new ArgumentOutOfRangeException(nameof(Length), "Must be null or in the range 1-5") : value;
    }

    internal int? LengthUnsafe
    {
        set => length = value;
    }

    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.Appenders.Add(LogLevelAppender.UnsafeFor(length));
    }

    public ILineToken Clone() => new LogLevelToken() { length = length };
}
