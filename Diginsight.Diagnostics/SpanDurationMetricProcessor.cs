using Microsoft.Extensions.Options;
using OpenTelemetry;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public sealed class SpanDurationMetricProcessor : BaseProcessor<Activity>
{
    private readonly IDiginsightActivitiesOptions activitiesOptions;
    private readonly ISpanDurationMetricSampler? sampler;
    private readonly ISpanDurationMetricProvider metricProvider;

    private Histogram<double>? metric;

    private Histogram<double> Metric => metric ??= metricProvider.Metric;

    public SpanDurationMetricProcessor(
        IOptions<DiginsightActivitiesOptions> activitiesOptions,
        ISpanDurationMetricSampler? sampler = null,
        ISpanDurationMetricProvider? metricProvider = null
    )
    {
        this.activitiesOptions = activitiesOptions.Value;
        this.sampler = sampler;
        this.metricProvider = metricProvider ?? new DefaultSpanDurationMetricProvider();
    }

    public override void OnStart(Activity activity) { }

    public override void OnEnd(Activity activity)
    {
        if (!(sampler?.ShouldRecord(activity, activity.GetCallerType()) ?? activitiesOptions.RecordSpanDurations))
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
