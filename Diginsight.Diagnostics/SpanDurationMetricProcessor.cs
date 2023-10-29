using OpenTelemetry;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public sealed class SpanDurationMetricProcessor : BaseProcessor<Activity>
{
    private static readonly Histogram<double> SpanDurationMetric = ObservabilityDefaults.Meter.CreateHistogram<double>("span_duration", "ms");

    public override void OnStart(Activity activity) { }

    public override void OnEnd(Activity activity)
    {
        SpanDurationMetric.Record(
            activity.Duration.TotalMilliseconds,
            new Tag("span_name", activity.OperationName),
            new Tag("status", activity.Status.ToString())
        );
    }
}
