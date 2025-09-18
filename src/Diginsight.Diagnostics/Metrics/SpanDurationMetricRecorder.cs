using Diginsight.Options;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public sealed class SpanDurationMetricRecorder : IActivityListenerLogic
{
    private readonly ILogger logger;
    private readonly IClassAwareOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor;
    private readonly IMeterFactory meterFactory;
    private readonly IMetricRecordingFilter? recordingFilter;
    private readonly IMetricRecordingEnricher? recordingEnricher;

    //private readonly Lazy<Histogram<double>> metricLazy;

    //private Histogram<double> Metric => metricLazy.Value;
    //private IMetricRecordingFilter RecordingFilter => recordingFilterLazy.Value;
    //private IMetricRecordingEnricher RecordingEnricher => recordingEnricherLazy.Value;

    public SpanDurationMetricRecorder(
        ILogger<SpanDurationMetricRecorder> logger,
        IClassAwareOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor,
        IMeterFactory meterFactory,
        IMetricRecordingFilter? recordingFilter,
        IMetricRecordingEnricher? recordingEnricher
    )
    {
        this.logger = logger;
        this.activitiesOptionsMonitor = activitiesOptionsMonitor;
        this.meterFactory = meterFactory;
        this.recordingFilter = recordingFilter;
        this.recordingEnricher = recordingEnricher;

        //IDiginsightActivitiesMetricOptions activitiesOptions = activitiesOptionsMonitor.CurrentValue;
        //var metricName = activitiesOptions.MetricName;

        //metricLazy = new Lazy<Histogram<double>>(
        //    () =>
        //    {
        //        IDiginsightActivitiesMetricOptions options = activitiesOptionsMonitor.CurrentValue;
        //        return meterFactory.Create(options.MeterName)
        //            .CreateHistogram<double>(options.MetricName, options.MetricUnit ?? "ms", options.MetricDescription);
        //    }
        //);

        //var metricFilter = serviceProvider.GetNamedService<IMetricRecordingFilter>(metricName);
        //this.metricFilter = metricFilter ?? serviceProvider.GetRequiredService<IMetricRecordingFilter>();

        //var metricEnricher = serviceProvider.GetNamedService<IMetricRecordingEnricher>(metricName);
        //this.metricEnricher = metricEnricher ?? serviceProvider.GetRequiredService<IMetricRecordingEnricher>();
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

            if (!(recordingFilter?.ShouldRecord(activity) ?? activitiesOptions.RecordSpanDurations))
                return;

            Tag nameTag = new("span_name", activityName);
            Tag statusTag = new("status", activity.Status.ToString());

            Tag[] tags = recordingEnricher is not null ? [ nameTag, statusTag, .. recordingEnricher.ExtractTags(activity) ] : [ nameTag, statusTag ];

            Histogram<double> metric;
            // TEMP Get named metric based on activitiesOptions.MetricName and callerType
            Unsafe.SkipInit(out metric);
            metric.Record(activity.Duration.TotalMilliseconds, tags);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unhandled exception while recording span duration metric of activity {ActivityName}", activityName);
        }
    }

    ActivitySamplingResult IActivityListenerLogic.Sample(ref ActivityCreationOptions<ActivityContext> creationOptions) => ActivitySamplingResult.AllData;
}
