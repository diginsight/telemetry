using System.Runtime.CompilerServices;

namespace Diginsight.Stringify;

public readonly struct InheritableThreshold
{
    public static readonly InheritableThreshold Unspecified = default;
    public static readonly InheritableThreshold Inherited = new (null, true);

    private readonly int? value;

    public bool IsInherited { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public InheritableThreshold(int value)
        : this(value < 0 ? throw new ArgumentOutOfRangeException(nameof(value), "Expected non-negative value") : value, false) { }

    private InheritableThreshold(int? value, bool isInherited)
    {
        this.value = value;
        IsInherited = isInherited;
    }

    public int? GetValue(Threshold finalFallback, params InheritableThreshold[] middleFallbacks)
    {
        foreach (InheritableThreshold threshold in middleFallbacks.Reverse().Prepend(this))
        {
            if (!threshold.IsInherited)
            {
                return threshold.value;
            }
        }

        return finalFallback.Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator InheritableThreshold(int value) => new (value);
}
