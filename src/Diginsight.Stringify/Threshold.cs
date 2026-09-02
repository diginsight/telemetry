using System.Runtime.CompilerServices;

namespace Diginsight.Stringify;

/// <summary>
/// Represents a non-negative optional threshold value.
/// </summary>
public readonly struct Threshold
{
    /// <summary>
    /// Represents an unspecified threshold.
    /// </summary>
    public static readonly Threshold Unspecified = default;

    /// <summary>
    /// Gets the threshold value.
    /// </summary>
    public int? Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Threshold" /> struct with the specified values.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is negative.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Threshold(int value)
        : this(value < 0 ? throw new ArgumentOutOfRangeException(nameof(value), "Expected non-negative value") : (int?)value) { }

    private Threshold(int? value)
    {
        Value = value;
    }

    /// <summary>
    /// Implicitly converts a value to a <see cref="Threshold" />.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The converted threshold.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Threshold(int value) => new (value);
}
