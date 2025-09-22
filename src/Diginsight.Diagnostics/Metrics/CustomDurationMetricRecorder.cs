using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public sealed class CustomDurationMetricRecorder : IActivityListenerLogic
{
    private readonly ILogger logger;
    private readonly IMetricRecordingFilter? recordingFilter;
    private readonly IMetricRecordingEnricher? recordingEnricher;

    public CustomDurationMetricRecorder(
        ILogger<CustomDurationMetricRecorder> logger,
        IMetricRecordingFilter? recordingFilter = null,
        IMetricRecordingEnricher? recordingEnricher = null
    )
    {
        this.logger = logger;
        this.recordingFilter = recordingFilter;
        this.recordingEnricher = recordingEnricher;
    }

#if !(NET || NETSTANDARD2_1_OR_GREATER)
    void IActivityListenerLogic.ActivityStarted(Activity activity) { }
#endif

    void IActivityListenerLogic.ActivityStopped(Activity activity)
    {
        try
        {
            double duration = activity.Duration.TotalMilliseconds;

            if (activity.GetCustomDurationMetric() is not { } instrument ||
                recordingFilter?.ShouldRecord(activity, instrument) == false)
            {
                return;
            }

            Tag[] tags = activity.GetCustomDurationMetricTags();
            Tag[] finalTags = recordingEnricher is not null ? tags.Concat(recordingEnricher.ExtractTags(activity, instrument)).ToArray() : tags;

            switch (instrument)
            {
                case Histogram<double> metric:
                    metric.Record(duration, finalTags);
                    break;

                case Histogram<long> metric:
                    metric.Record((long)duration, finalTags);
                    break;

                default:
                    throw new UnreachableException($"Unrecognized {nameof(Instrument)}");
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unhandled exception while recording custom duration metric of activity {ActivityName}", activity.OperationName);
        }
    }

    ActivitySamplingResult IActivityListenerLogic.Sample(ref ActivityCreationOptions<ActivityContext> creationOptions) => ActivitySamplingResult.AllData;
}
