using System.Runtime.CompilerServices;

namespace Diginsight.Stringify;

public readonly struct Threshold
{
    public static readonly Threshold Unspecified = default;

    public int? Value { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Threshold(int value)
        : this(value < 0 ? throw new ArgumentOutOfRangeException(nameof(value), "Expected non-negative value") : (int?)value) { }

    private Threshold(int? value)
    {
        Value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Threshold(int value) => new (value);
}
