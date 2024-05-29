using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public class DefaultSpanDurationMetricRecorderSettings : ISpanDurationMetricRecorderSettings
{
    private Histogram<double>? metric;

    public Meter? Meter { get; set; }

    public string? MetricName { get; set; }
    public string? MetricUnit { get; set; }
    public string? MetricDescription { get; set; }

    public virtual Histogram<double> Metric
    {
        get
        {
            return metric ??= (Meter ?? DiginsightDefaults.Meter)
                .CreateHistogram<double>(MetricName ?? "diginsight.span_duration", MetricUnit ?? "ms", MetricDescription);
        }
    }

    public virtual bool? ShouldRecord(Activity activity) => null;

    public virtual Tags ExtractTags(Activity activity) => [ ];
}
