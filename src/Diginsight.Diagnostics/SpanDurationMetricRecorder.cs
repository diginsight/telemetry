using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents an activity listener logic that records span duration metrics.
/// </summary>
public sealed class SpanDurationMetricRecorder : IActivityListenerLogic
{
    private readonly ILogger logger;
    private readonly IOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor;
    private readonly Lazy<Histogram<double>> metricLazy;
    private readonly IMetricRecordingFilter? recordingFilter;
    private readonly IMetricRecordingEnricher? recordingEnricher;

    /// <summary>
    /// DI constructor.
    /// </summary>
    public SpanDurationMetricRecorder(
        ILogger<SpanDurationMetricRecorder> logger,
        IOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor,
        IMeterFactory meterFactory,
        IMetricRecordingFilter? recordingFilter = null,
        IMetricRecordingEnricher? recordingEnricher = null
    )
    {
        this.logger = logger;
        this.activitiesOptionsMonitor = activitiesOptionsMonitor;
        this.recordingFilter = recordingFilter;
        this.recordingEnricher = recordingEnricher;

        IMetricRecordingOptions metricOptions = activitiesOptionsMonitor.CurrentValue;
        metricLazy = new Lazy<Histogram<double>>(
            () => meterFactory
                .Create(metricOptions.MeterName)
                .CreateHistogram<double>(metricOptions.MetricName, "ms", metricOptions.MetricDescription)
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
            bool record = ((IMetricRecordingOptions)activitiesOptionsMonitor.CurrentValue).Record;

            if (!(recordingFilter?.ShouldRecord(activity, metric) ?? record))
                return;

            Tag nameTag = new ("span_name", activityName);
            Tag statusTag = new ("status", activity.Status.ToString());

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
