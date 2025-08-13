using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public sealed class CustomDurationMetricRecorder : IActivityListenerLogic
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

#if !(NET || NETSTANDARD2_1_OR_GREATER)
    void IActivityListenerLogic.ActivityStarted(Activity activity) { }
#endif

    void IActivityListenerLogic.ActivityStopped(Activity activity)
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

    ActivitySamplingResult IActivityListenerLogic.Sample(ref ActivityCreationOptions<ActivityContext> creationOptions) => ActivitySamplingResult.AllData;
}
