using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public sealed class DefaultSpanDurationMetricProvider : ISpanDurationMetricProvider
{
    private Histogram<double>? metric;

    public Meter? Meter { get; set; }

    public string? MetricName { get; set; }

    public Histogram<double> Metric => metric ??= (Meter ?? ObservabilityDefaults.Meter).CreateHistogram<double>(MetricName ?? "diginsight.span_duration", "ms");
}
