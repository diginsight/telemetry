using OpenTelemetry;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public sealed class DurationMetricProcessor : BaseProcessor<Activity>
{
    private static readonly Histogram<double> SpanDurationMetric = ObservabilityDefaults.Meter.CreateHistogram<double>("span_duration", "ms");

    public override void OnStart(Activity activity) { }

    public override void OnEnd(Activity activity)
    {
        double duration = activity.Duration.TotalMilliseconds;

        // TODO move tag names in an appropriate class
        SpanDurationMetric.Record(
            duration,
            new Tag("span_name", activity.OperationName),
            new Tag("status", activity.Status.ToString())
        );

        switch (activity.GetCustomProperty(ActivityCustomPropertyNames.DurationMetric))
        {
            case Histogram<double> durationMetric:
                durationMetric.Record(duration, activity.GetDurationMetricTags());
                break;

            case Histogram<long> durationMetric:
                durationMetric.Record((long)duration, activity.GetDurationMetricTags());
                break;

            case null:
                break;

            default:
                throw new InvalidOperationException("Invalid duration metric in activity");
        }
    }
}
