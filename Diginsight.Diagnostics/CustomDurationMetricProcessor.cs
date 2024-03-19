using Microsoft.Extensions.Logging;
using OpenTelemetry;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public sealed class CustomDurationMetricProcessor : BaseProcessor<Activity>
{
    private readonly ILogger logger;
    private readonly ICustomDurationMetricSampler? sampler;

    public CustomDurationMetricProcessor(
        ILogger<CustomDurationMetricProcessor> logger,
        ICustomDurationMetricSampler? sampler = null
    )
    {
        this.logger = logger;
        this.sampler = sampler;
    }

    public override void OnEnd(Activity activity)
    {
        try
        {
            double duration = activity.Duration.TotalMilliseconds;

            bool ShouldRecord(Instrument instrument) => sampler?.ShouldRecord(activity, activity.GetCallerType(), instrument) ?? true;

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
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unhandled exception while recording custom duration metric of activity {ActivityName}", activity.OperationName);
        }
    }
}
