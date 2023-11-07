using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public static class MeterExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimerHistogram CreateTimer(this Meter meter, string name, string? unit = "ms", string? description = null)
    {
        return new TimerHistogram(meter, name, unit, description);
    }
}
