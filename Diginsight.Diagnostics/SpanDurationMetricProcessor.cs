using OpenTelemetry;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public sealed class SpanDurationMetricProcessor : BaseProcessor<Activity>
{
    private static readonly Histogram<double> Metric = ObservabilityDefaults.Meter.CreateHistogram<double>("span_duration", "ms");

    private readonly IEnumerable<ISpanDurationMetricSampler> samplers;

    public SpanDurationMetricProcessor(IEnumerable<ISpanDurationMetricSampler> samplers)
    {
        this.samplers = samplers;
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

    public static bool IsSpanDurationMetric(Instrument instrument) => instrument == Metric;
}
