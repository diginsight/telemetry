using System.Diagnostics.Metrics;
using System.Reflection;

namespace Diginsight.Stringify;

internal static class SelfObservabilityUtils
{
    public static readonly Meter Meter = new (Assembly.GetExecutingAssembly().GetName().Name!);
}
