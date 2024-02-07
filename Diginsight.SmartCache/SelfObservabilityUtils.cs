using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.SmartCache;

internal static class SelfObservabilityUtils
{
    public static readonly ActivitySource ActivitySource = new ("Diginsight.Cache");
    public static readonly Meter Meter = new ("Diginsight.Cache");
}
