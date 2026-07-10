using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public sealed class SpanDurationMetricRecorder : IActivityListenerLogic
{
    private readonly ILogger logger;
    private readonly IOptions<DiginsightActivitiesOptions> activitiesOptions;
    private readonly IMeterFactory meterFactory;
    private readonly IMetricRecordingFilter? recordingFilter;
    private readonly IMetricRecordingEnricher? recordingEnricher;

    private IMetricRecordingOptions MetricOptions =>
        field ??= activitiesOptions.Value.Freeze();

    private Histogram<double> Metric => meterFactory
        .Create(MetricOptions.MeterName)
        .CreateHistogram<double>(MetricOptions.MetricName, "ms", MetricOptions.MetricDescription);

    public SpanDurationMetricRecorder(
        ILogger<SpanDurationMetricRecorder> logger,
        IOptions<DiginsightActivitiesOptions> activitiesOptions,
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
            if (!(recordingFilter?.ShouldRecord(activity, Metric) ?? MetricOptions.Record))
                return;

            Tag nameTag = new ("span_name", activityName);
            Tag statusTag = new ("status", activity.Status.ToString());

            Tag[] tags = recordingEnricher is not null
                ? [ nameTag, statusTag, .. recordingEnricher.ExtractTags(activity, Metric) ]
                : [ nameTag, statusTag ];

            Metric.Record(activity.Duration.TotalMilliseconds, tags);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unhandled exception while recording span duration metric of activity {ActivityName}", activityName);
        }
    }

    ActivitySamplingResult IActivityListenerLogic.Sample(ref ActivityCreationOptions<ActivityContext> creationOptions) => ActivitySamplingResult.AllData;
}
