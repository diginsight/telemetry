using OpenTelemetry;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public sealed class CustomDurationMetricProcessor : BaseProcessor<Activity>
{
    private readonly ICustomDurationMetricSampler? sampler;

    public CustomDurationMetricProcessor(ICustomDurationMetricSampler? sampler = null)
    {
        this.sampler = sampler;
    }

    public override void OnStart(Activity activity) { }

    public override void OnEnd(Activity activity)
    {
        double duration = activity.Duration.TotalMilliseconds;

        bool ShouldRecord(Instrument instrument) => sampler?.ShouldRecord(activity, instrument) ?? true;

        switch (activity.GetCustomProperty(ActivityCustomPropertyNames.DurationMetric))
        {
            case Histogram<double> durationMetric:
                if (ShouldRecord(durationMetric))
                {
                    durationMetric.Record(duration, activity.GetDurationMetricTags());
                }
                break;

            case Histogram<long> durationMetric:
                if (ShouldRecord(durationMetric))
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
