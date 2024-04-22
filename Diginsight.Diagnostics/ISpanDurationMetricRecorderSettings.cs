using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public interface ISpanDurationMetricRecorderSettings
{
    Histogram<double> Metric { get; }

    bool? ShouldRecord(Activity activity);

    Tags ExtractTags(Activity activity);
}
