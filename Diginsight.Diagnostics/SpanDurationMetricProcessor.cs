using Diginsight.CAOptions;
using OpenTelemetry;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public sealed class SpanDurationMetricProcessor : BaseProcessor<Activity>
{
    private readonly IClassAwareOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor;
    private readonly ISpanDurationMetricSampler? sampler;
    private readonly ISpanDurationMetricProvider metricProvider;

    private Histogram<double>? metric;

    private Histogram<double> Metric => metric ??= metricProvider.Metric;

    public SpanDurationMetricProcessor(
        IClassAwareOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor,
        ISpanDurationMetricSampler? sampler = null,
        ISpanDurationMetricProvider? metricProvider = null
    )
    {
        this.activitiesOptionsMonitor = activitiesOptionsMonitor;
        this.sampler = sampler;
        this.metricProvider = metricProvider ?? new DefaultSpanDurationMetricProvider();
    }

    public override void OnStart(Activity activity) { }

    public override void OnEnd(Activity activity)
    {
        Type? callerType = activity.GetCallerType();
        IDiginsightActivitiesOptions activitiesOptions = activitiesOptionsMonitor.Get(callerType ?? ClassAwareOptions.NoType);
        if (!(sampler?.ShouldRecord(activity, callerType) ?? activitiesOptions.RecordSpanDurations))
        {
            return;
        }

        Metric.Record(
            activity.Duration.TotalMilliseconds,
            new Tag("span_name", activity.OperationName),
            new Tag("status", activity.Status.ToString())
        );
    }

    public bool IsSpanDurationMetric(Instrument instrument) => instrument == Metric;
}
