namespace Diginsight.Diagnostics.TextWriting;

public sealed class LogLevelToken : ILineToken
{
    internal int? length;

    public int? Length
    {
        get => length;
        set => length = value is < 1 or > 5 ? throw new ArgumentOutOfRangeException(nameof(Length), "Length must be null or in the range 1-5") : value;
    }
}
