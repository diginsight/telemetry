using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.SmartCache;

internal static class SelfObservabilityUtils
{
    public static readonly ActivitySource ActivitySource;
    public static readonly Meter Meter;

    static SelfObservabilityUtils()
    {
        string ns = typeof(SelfObservabilityUtils).Namespace!;

        ActivitySource = new ActivitySource(ns);
        Meter = new Meter(ns);
    }
}
