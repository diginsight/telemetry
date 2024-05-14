using System.Runtime.CompilerServices;

namespace Diginsight.Strings;

public readonly struct LogThreshold
{
    public static readonly LogThreshold Unspecified = default;

    public int? Value { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LogThreshold(int value)
        : this(value < 0 ? throw new ArgumentOutOfRangeException(nameof(value), "Expected non-negative value") : (int?)value) { }

    private LogThreshold(int? value)
    {
        Value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator LogThreshold(int value) => new (value);
}
