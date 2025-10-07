using Diginsight.Options;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public sealed class SpanDurationMetricRecorder : IActivityListenerLogic
{
    private readonly ILogger logger;
    private readonly IClassAwareOptions<DiginsightActivitiesOptions> activitiesOptions;
    private readonly IMetricRecordingFilter? recordingFilter;
    private readonly IMetricRecordingEnricher? recordingEnricher;
    private readonly Lazy<Histogram<double>> metricLazy;

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
        this.recordingFilter = recordingFilter;
        this.recordingEnricher = recordingEnricher;

        metricLazy = new Lazy<Histogram<double>>(
            () =>
            {
                IMetricRecordingOptions metricOptions = activitiesOptions.Value.Freeze();
                return meterFactory
                    .Create(metricOptions.MeterName)
                    .CreateHistogram<double>(metricOptions.MetricName, "ms", metricOptions.MetricDescription);
            }
        );
    }

#if !(NET || NETSTANDARD2_1_OR_GREATER)
    void IActivityListenerLogic.ActivityStarted(Activity activity) { }
#endif

    void IActivityListenerLogic.ActivityStopped(Activity activity)
    {
        string activityName = activity.OperationName;

        try
        {
            Histogram<double> metric = metricLazy.Value;
            IMetricRecordingOptions metricOptions = activitiesOptions.Get(activity.GetCallerType()).Freeze();

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
