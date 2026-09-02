using System.Runtime.CompilerServices;

namespace Diginsight.Stringify;

/// <summary>
/// Represents a threshold that can specify a value, inherit from another threshold, or remain unspecified.
/// </summary>
public readonly struct InheritableThreshold
{
    /// <summary>
    /// Represents an unspecified threshold.
    /// </summary>
    public static readonly InheritableThreshold Unspecified = default;
    /// <summary>
    /// Represents an inherited threshold.
    /// </summary>
    public static readonly InheritableThreshold Inherited = new (null, true);

    private readonly int? value;

    /// <summary>
    /// Gets whether the threshold is inherited.
    /// </summary>
    public bool IsInherited { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InheritableThreshold" /> struct.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is negative.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public InheritableThreshold(int value)
        : this(value < 0 ? throw new ArgumentOutOfRangeException(nameof(value), "Expected non-negative value") : value, false) { }

    private InheritableThreshold(int? value, bool isInherited)
    {
        this.value = value;
        IsInherited = isInherited;
    }

    /// <summary>
    /// Resolves the threshold value by applying inherited and fallback thresholds.
    /// </summary>
    /// <param name="finalFallback">The final fallback threshold.</param>
    /// <param name="middleFallbacks">The intermediate fallback thresholds, evaluated in reverse order after the current instance and before <paramref name="finalFallback" />.</param>
    /// <returns>The resolved threshold value.</returns>
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

    /// <summary>
    /// Implicitly converts a value to a <see cref="InheritableThreshold" />.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The converted threshold.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator InheritableThreshold(int value) => new (value);
}
