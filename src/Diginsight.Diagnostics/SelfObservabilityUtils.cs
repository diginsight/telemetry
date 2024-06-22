using System.Diagnostics.Metrics;
using System.Reflection;

namespace Diginsight.Diagnostics;

internal static class SelfObservabilityUtils
{
    public static readonly Meter Meter = new (Assembly.GetExecutingAssembly().GetName().Name!);
}
