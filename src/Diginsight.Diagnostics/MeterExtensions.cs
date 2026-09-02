using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

/// <summary>
/// Provides extension methods for <see cref="Meter" /> instances.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class MeterExtensions
{
    /// <summary>
    /// Creates a timer histogram from the specified meter.
    /// </summary>
    /// <param name="meter">The meter used to create the histogram.</param>
    /// <param name="name">The histogram name.</param>
    /// <param name="unit">The histogram unit.</param>
    /// <param name="description">The histogram description.</param>
    /// <returns>The created timer histogram.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimerHistogram CreateTimer(this Meter meter, string name, string? unit = "ms", string? description = null)
    {
        return new TimerHistogram(meter, name, unit, description);
    }
}
