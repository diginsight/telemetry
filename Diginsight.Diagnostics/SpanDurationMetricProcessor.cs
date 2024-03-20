using Diginsight.CAOptions;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public sealed class SpanDurationMetricProcessor : BaseProcessor<Activity>
{
    private readonly ILogger logger;
    private readonly IClassAwareOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor;
    private readonly ISpanDurationMetricProcessorSettings settings;

    private Histogram<double>? metric;

    private Histogram<double> Metric => metric ??= settings.Metric;

    public SpanDurationMetricProcessor(
        ILogger<SpanDurationMetricProcessor> logger,
        IClassAwareOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor,
        ISpanDurationMetricProcessorSettings? settings = null
    )
    {
        this.logger = logger;
        this.activitiesOptionsMonitor = activitiesOptionsMonitor;
        this.settings = settings ?? new DefaultSpanDurationMetricProcessorSettings();
    }

    public override void OnEnd(Activity activity)
    {
        string activityName = activity.OperationName;

        try
        {
            Type? callerType = activity.GetCallerType();
            IDiginsightActivitiesOptions activitiesOptions = activitiesOptionsMonitor.Get(callerType);
            if (!(settings.ShouldRecord(activity) ?? activitiesOptions.RecordSpanDurations))
            {
                return;
            }

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

    public bool IsSpanDurationMetric(Instrument instrument) => instrument == Metric;
}
