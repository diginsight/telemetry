using System.Diagnostics.Metrics;

namespace Diginsight;

internal static class AutoObservabilityUtils
{
    public static readonly Meter Meter = new Meter("Diginsight");
}
