using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public class DefaultSpanDurationMetricRecorderSettings : ISpanDurationMetricRecorderSettings
{
    private Histogram<double>? metric;

    public Meter? Meter { get; set; }

    public string? MetricName { get; set; }

    public virtual Histogram<double> Metric
    {
        get { return metric ??= (Meter ?? DiginsightDefaults.Meter).CreateHistogram<double>(MetricName ?? "diginsight.span_duration", "ms"); }
    }

    public virtual bool? ShouldRecord(Activity activity) => null;

    public virtual Tags ExtractTags(Activity activity) => Enumerable.Empty<Tag>();
}
