using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public
#if NET
    interface ICustomMetrics<TSelf>
    where TSelf : ICustomMetrics<TSelf>
#else
    abstract class CustomMetrics
#endif
{
    public
#if NET
        static
#endif
        Meter Meter => new (
#if NET
        TSelf.ObservabilityName
#else
        ObservabilityName
#endif
    );

    public
#if NET
        static
#endif
        abstract string ObservabilityName { get; }

    public
#if NET
        static
#endif
        virtual (string InstrumentName, MetricStreamConfiguration MetricStreamConfiguration)[] Views => [ ];
}
