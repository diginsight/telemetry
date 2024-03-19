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
    private readonly ISpanDurationMetricSampler? sampler;
    private readonly ISpanDurationMetricProvider metricProvider;

    private Histogram<double>? metric;

    private Histogram<double> Metric => metric ??= metricProvider.Metric;

    public SpanDurationMetricProcessor(
        ILogger<SpanDurationMetricProcessor> logger,
        IClassAwareOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor,
        ISpanDurationMetricSampler? sampler = null,
        ISpanDurationMetricProvider? metricProvider = null
    )
    {
        this.logger = logger;
        this.activitiesOptionsMonitor = activitiesOptionsMonitor;
        this.sampler = sampler;
        this.metricProvider = metricProvider ?? new DefaultSpanDurationMetricProvider();
    }

    public override void OnEnd(Activity activity)
    {
        string activityName = activity.OperationName;

        try
        {
            Type? callerType = activity.GetCallerType();
            IDiginsightActivitiesOptions activitiesOptions = activitiesOptionsMonitor.Get(callerType ?? ClassAwareOptions.NoType);
            if (!(sampler?.ShouldRecord(activity, callerType) ?? activitiesOptions.RecordSpanDurations))
            {
                return;
            }

            Metric.Record(
                activity.Duration.TotalMilliseconds,
                new Tag("span_name", activityName),
                new Tag("status", activity.Status.ToString())
            );
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unhandled exception while recording span duration metric of activity {ActivityName}", activityName);
        }
    }

    public bool IsSpanDurationMetric(Instrument instrument) => instrument == Metric;
}
