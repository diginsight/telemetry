using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public sealed class CustomDurationMetricRecorder
{
    private readonly ILogger logger;
    private readonly ICustomDurationMetricRecorderSettings? settings;

    public CustomDurationMetricRecorder(
        ILogger<CustomDurationMetricRecorder> logger,
        ICustomDurationMetricRecorderSettings? settings = null
    )
    {
        this.logger = logger;
        this.settings = settings;
    }

    public void InstallActivityListener(Func<ActivitySource, bool> shouldListenTo)
    {
        ActivitySource.AddActivityListener(
            new ActivityListener
            {
                ActivityStopped = OnEnd,
                Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ShouldListenTo = shouldListenTo,
            }
        );
    }

    private void OnEnd(Activity activity)
    {
        try
        {
            double duration = activity.Duration.TotalMilliseconds;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool ShouldRecord(Instrument instrument) => settings?.ShouldRecord(activity, instrument) ?? true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            Tag[] ExtractTags(Instrument instrument)
            {
                Tag[] tags = activity.GetCustomDurationMetricTags();
                return settings is not null ? tags.Concat(settings.ExtractTags(activity, instrument)).ToArray() : tags;
            }

            switch (activity.GetCustomProperty(ActivityCustomPropertyNames.CustomDurationMetric))
            {
                case Histogram<double> metric:
                    if (ShouldRecord(metric))
                    {
                        metric.Record(duration, ExtractTags(metric));
                    }
                    break;

                case Histogram<long> metric:
                    if (ShouldRecord(metric))
                    {
                        metric.Record((long)duration, ExtractTags(metric));
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
