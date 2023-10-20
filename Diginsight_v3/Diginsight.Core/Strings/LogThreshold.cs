namespace Diginsight.Strings;

public readonly struct LogThreshold
{
    public static readonly LogThreshold Unspecified = default;

    public int? Value { get; }

    public LogThreshold(int value)
        : this(value < 0 ? throw new ArgumentOutOfRangeException(nameof(value), "expected non-negative value") : (int?)value) { }

    private LogThreshold(int? value)
    {
        Value = value;
    }

    public static implicit operator LogThreshold(int value) => new (value);
}
