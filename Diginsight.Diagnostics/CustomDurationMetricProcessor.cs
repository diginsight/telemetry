using OpenTelemetry;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public sealed class CustomDurationMetricProcessor : BaseProcessor<Activity>
{
    private readonly IEnumerable<ICustomDurationMetricSampler> samplers;

    public CustomDurationMetricProcessor(IEnumerable<ICustomDurationMetricSampler> samplers)
    {
        this.samplers = samplers;
    }

    public override void OnStart(Activity activity) { }

    public override void OnEnd(Activity activity)
    {
        double duration = activity.Duration.TotalMilliseconds;

        switch (activity.GetCustomProperty(ActivityCustomPropertyNames.DurationMetric))
        {
            case Histogram<double> durationMetric:
                if (samplers.All(s => s.ShouldRecord(activity, durationMetric)))
                {
                    durationMetric.Record(duration, activity.GetDurationMetricTags());
                }
                break;

            case Histogram<long> durationMetric:
                if (samplers.All(s => s.ShouldRecord(activity, durationMetric)))
                {
                    durationMetric.Record((long)duration, activity.GetDurationMetricTags());
                }
                break;

            case null:
                break;

            default:
                throw new InvalidOperationException("Invalid duration metric in activity");
        }
    }
}
