using OpenTelemetry;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public sealed class SpanDurationMetricProcessor : BaseProcessor<Activity>
{
    private readonly IEnumerable<ISpanDurationMetricSampler> samplers;
    private readonly ISpanDurationMetricProvider metricProvider;

    private Histogram<double>? metric;

    private Histogram<double> Metric => metric ??= metricProvider.Metric;

    public SpanDurationMetricProcessor(
        IEnumerable<ISpanDurationMetricSampler> samplers,
        ISpanDurationMetricProvider? metricProvider = null
    )
    {
        this.samplers = samplers;
        this.metricProvider = metricProvider ?? new DefaultSpanDurationMetricProvider();
    }

    public override void OnStart(Activity activity) { }

    public override void OnEnd(Activity activity)
    {
        Metric.Record(
            activity.Duration.TotalMilliseconds,
            new Tag("span_name", activity.OperationName),
            new Tag("status", activity.Status.ToString())
        );
    }

    public bool IsSpanDurationMetric(Instrument instrument) => instrument == Metric;
}
