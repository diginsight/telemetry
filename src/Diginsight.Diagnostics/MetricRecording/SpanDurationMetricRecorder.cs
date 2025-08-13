using Diginsight.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public sealed class SpanDurationMetricRecorder : IActivityListenerLogic
{
    private readonly ILogger logger;
    private readonly IClassAwareOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor;
    private readonly IMeterFactory meterFactory;
    private readonly IMetricRecordingFilter? metricFilter;
    private readonly IMetricRecordingEnricher? metricEnricher;

    private readonly Lazy<Histogram<double>> lazyMetric;
    private Histogram<double> Metric => lazyMetric.Value;

    public SpanDurationMetricRecorder(
        IServiceProvider serviceProvider,
        ILogger<SpanDurationMetricRecorder> logger,
        IClassAwareOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor,
        IMeterFactory meterFactory
    )
    {
        this.logger = logger;
        this.activitiesOptionsMonitor = activitiesOptionsMonitor;
        this.meterFactory = meterFactory;

        IDiginsightActivitiesMetricOptions activitiesOptions = activitiesOptionsMonitor.CurrentValue;
        var metricName = activitiesOptions.MetricName;

        // get metric (deplayed) with lazy initialization
        this.lazyMetric = new Lazy<Histogram<double>>(() => {
            IDiginsightActivitiesMetricOptions options = activitiesOptionsMonitor.CurrentValue;
            return meterFactory.Create(options.MeterName)
                               .CreateHistogram<double>(options.MetricName, options.MetricUnit ?? "ms", options.MetricDescription);
        });

        var metricFilter = serviceProvider.GetNamedService<IMetricRecordingFilter>(metricName);
        this.metricFilter = metricFilter ?? serviceProvider.GetRequiredService<IMetricRecordingFilter>();

        var metricEnricher = serviceProvider.GetNamedService<IMetricRecordingEnricher>(metricName);
        this.metricEnricher = metricEnricher ?? serviceProvider.GetRequiredService<IMetricRecordingEnricher>();
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
            if (!(metricFilter?.ShouldRecord(activity) ?? activitiesOptions.RecordSpanDurations))
                return;

            //Tag traceId = new("trace_id", activity.TraceId.ToString());
            Tag nameTag = new("span_name", activityName);
            Tag statusTag = new("status", activity.Status.ToString());
            Tag[] tags = metricEnricher is not null ? [nameTag, statusTag, .. metricEnricher.ExtractTags(activity)] : [nameTag, statusTag];

            Metric.Record(activity.Duration.TotalMilliseconds, tags);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unhandled exception while recording span duration metric of activity {ActivityName}", activityName);
        }
    }

    ActivitySamplingResult IActivityListenerLogic.Sample(ref ActivityCreationOptions<ActivityContext> creationOptions) => ActivitySamplingResult.AllData;
}
