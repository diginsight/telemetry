using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents a contract for custom metric metadata exposed to OpenTelemetry.
/// </summary>
/// <typeparam name="TSelf">The custom metrics type.</typeparam>
public
#if NET
    interface ICustomMetrics<TSelf>
    where TSelf : ICustomMetrics<TSelf>
#else
    abstract class CustomMetrics
#endif
{
    /// <summary>
    /// Gets the meter used to publish custom metrics.
    /// </summary>
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

    /// <summary>
    /// Gets the observability name used as the meter name.
    /// </summary>
    public
#if NET
        static
#endif
        abstract string ObservabilityName { get; }

    /// <summary>
    /// Gets the metric stream configurations associated with instruments.
    /// </summary>
    public
#if NET
        static
#endif
        virtual (string InstrumentName, MetricStreamConfiguration MetricStreamConfiguration)[] Views => [ ];
}
