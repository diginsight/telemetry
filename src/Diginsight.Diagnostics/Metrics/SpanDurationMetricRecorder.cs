using Diginsight.Options;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public sealed class SpanDurationMetricRecorder : IActivityListenerLogic
{
    private readonly ILogger logger;
    private readonly IClassAwareOptions<DiginsightActivitiesOptions> activitiesOptions;
    private readonly IMeterFactory meterFactory;
    private readonly IMetricRecordingFilter? recordingFilter;
    private readonly IMetricRecordingEnricher? recordingEnricher;

    public SpanDurationMetricRecorder(
        ILogger<SpanDurationMetricRecorder> logger,
        IClassAwareOptions<DiginsightActivitiesOptions> activitiesOptions,
        IMeterFactory meterFactory,
        IMetricRecordingFilter? recordingFilter = null,
        IMetricRecordingEnricher? recordingEnricher = null
    )
    {
        this.logger = logger;
        this.activitiesOptions = activitiesOptions;
        this.meterFactory = meterFactory;
        this.recordingFilter = recordingFilter;
        this.recordingEnricher = recordingEnricher;
    }

#if !(NET || NETSTANDARD2_1_OR_GREATER)
    void IActivityListenerLogic.ActivityStarted(Activity activity) { }
#endif

    void IActivityListenerLogic.ActivityStopped(Activity activity)
    {
        string activityName = activity.OperationName;

        try
        {
            Type? callerType = activity.GetCallerType();
            IDiginsightActivitiesSpanDurationOptions metricOptions = activitiesOptions.Get(callerType);

            Histogram<double> metric = meterFactory
                .Create(metricOptions.MeterName)
                .CreateHistogram<double>(metricOptions.MetricName, "ms", metricOptions.MetricDescription);

            if (!(recordingFilter?.ShouldRecord(activity, metric) ?? metricOptions.Record))
                return;

            Tag nameTag = new("span_name", activityName);
            Tag statusTag = new("status", activity.Status.ToString());

            Tag[] tags = recordingEnricher is not null
                ? [ nameTag, statusTag, .. recordingEnricher.ExtractTags(activity, metric) ]
                : [ nameTag, statusTag ];

            metric.Record(activity.Duration.TotalMilliseconds, tags);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unhandled exception while recording span duration metric of activity {ActivityName}", activityName);
        }
    }

    ActivitySamplingResult IActivityListenerLogic.Sample(ref ActivityCreationOptions<ActivityContext> creationOptions) => ActivitySamplingResult.AllData;
}
