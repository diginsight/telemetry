namespace Diginsight.Strings;

public readonly struct InheritableLogThreshold
{
    public static readonly InheritableLogThreshold Unspecified = default;
    public static readonly InheritableLogThreshold Inherited = new (null, true);

    private readonly int? value;

    public bool IsInherited { get; }

    public InheritableLogThreshold(int value)
        : this(value < 0 ? throw new ArgumentOutOfRangeException(nameof(value), "Expected non-negative value") : value, false) { }

    private InheritableLogThreshold(int? value, bool isInherited)
    {
        this.value = value;
        IsInherited = isInherited;
    }

    public int? GetValue(LogThreshold finalFallback, params InheritableLogThreshold[] middleFallbacks)
    {
        foreach (InheritableLogThreshold threshold in middleFallbacks.Reverse().Prepend(this))
        {
            if (!threshold.IsInherited)
            {
                return threshold.value;
            }
        }

        return finalFallback.Value;
    }

    public static implicit operator InheritableLogThreshold(int value) => new (value);
}
