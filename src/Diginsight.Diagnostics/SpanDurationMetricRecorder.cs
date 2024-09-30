using Diginsight.Options;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public sealed class SpanDurationMetricRecorder : IActivityListenerLogic
{
    private readonly ILogger logger;
    private readonly IClassAwareOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor;
    private readonly IMeterFactory meterFactory;
    private readonly ISpanDurationMetricRecorderSettings? settings;

    private Histogram<double>? metric;

    private Histogram<double> Metric
    {
        get
        {
            if (metric is { } metric0)
            {
                return metric0;
            }

            IDiginsightActivitiesMetricOptions activitiesOptions = activitiesOptionsMonitor.CurrentValue;
            return metric = meterFactory.Create(activitiesOptions.MeterName)
                .CreateHistogram<double>(activitiesOptions.MetricName, activitiesOptions.MetricUnit ?? "ms", activitiesOptions.MetricDescription);
        }
    }

    public SpanDurationMetricRecorder(
        ILogger<SpanDurationMetricRecorder> logger,
        IClassAwareOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor,
        IMeterFactory meterFactory,
        ISpanDurationMetricRecorderSettings? settings = null
    )
    {
        this.logger = logger;
        this.activitiesOptionsMonitor = activitiesOptionsMonitor;
        this.meterFactory = meterFactory;
        this.settings = settings;
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
            IDiginsightActivitiesMetricOptions activitiesOptions = activitiesOptionsMonitor.Get(callerType);
            if (!(settings?.ShouldRecord(activity) ?? activitiesOptions.RecordSpanDurations))
                return;

            Tag nameTag = new ("span_name", activityName);
            Tag statusTag = new ("status", activity.Status.ToString());
            Tag[] tags = settings is not null ? [ nameTag, statusTag, ..settings.ExtractTags(activity) ] : [ nameTag, statusTag ];

            Metric.Record(activity.Duration.TotalMilliseconds, tags);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unhandled exception while recording span duration metric of activity {ActivityName}", activityName);
        }
    }

    ActivitySamplingResult IActivityListenerLogic.Sample(ref ActivityCreationOptions<ActivityContext> creationOptions) => ActivitySamplingResult.AllData;
}
