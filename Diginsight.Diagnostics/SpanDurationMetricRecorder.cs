using Diginsight.CAOptions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public sealed class SpanDurationMetricRecorder : ISpanDurationMetricRecorder
{
    private readonly ILogger logger;
    private readonly IClassAwareOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor;
    private readonly ISpanDurationMetricRecorderSettings settings;

    private Histogram<double>? metric;

    private Histogram<double> Metric => metric ??= settings.Metric;

    public SpanDurationMetricRecorder(
        ILogger<SpanDurationMetricRecorder> logger,
        IClassAwareOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor,
        ISpanDurationMetricRecorderSettings? settings = null
    )
    {
        this.logger = logger;
        this.activitiesOptionsMonitor = activitiesOptionsMonitor;
        this.settings = settings ?? new DefaultSpanDurationMetricRecorderSettings();
    }

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
    void IActivityListenerLogic.ActivityStarted(Activity activity) { }
#endif

    void IActivityListenerLogic.ActivityStopped(Activity activity)
    {
        string activityName = activity.OperationName;

        try
        {
            Type? callerType = activity.GetCallerType();
            IDiginsightActivitiesOptions activitiesOptions = activitiesOptionsMonitor.Get(callerType);
            if (!(settings.ShouldRecord(activity) ?? activitiesOptions.RecordSpanDurations))
                return;

            Metric.Record(
                activity.Duration.TotalMilliseconds,
                [ new Tag("span_name", activityName), new Tag("status", activity.Status.ToString()), ..settings.ExtractTags(activity) ]
            );
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unhandled exception while recording span duration metric of activity {ActivityName}", activityName);
        }
    }

    ActivitySamplingResult IActivityListenerLogic.Sample(ref ActivityCreationOptions<ActivityContext> creationOptions) => ActivitySamplingResult.AllData;

    public bool IsSpanDurationMetric(Instrument instrument) => instrument == Metric;
}
