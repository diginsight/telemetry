using System.Diagnostics.Metrics;

namespace Diginsight;

internal static class SelfObservabilityUtils
{
    public static readonly Meter Meter = new Meter("Diginsight");
}
