using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

#if NET7_0_OR_GREATER
public interface ICustomMetrics<TSelf>
    where TSelf : ICustomMetrics<TSelf>
{
    public static Meter Meter => new (TSelf.ObservabilityName);

    public static abstract string ObservabilityName { get; }

    public static virtual (string InstrumentName, MetricStreamConfiguration MetricStreamConfiguration)[] Views => [ ];
}
#else
public abstract class CustomMetrics
{
    public Meter Meter => new (ObservabilityName);

    public abstract string ObservabilityName { get; }

    public virtual (string InstrumentName, MetricStreamConfiguration MetricStreamConfiguration)[] Views => [ ];
}
#endif
