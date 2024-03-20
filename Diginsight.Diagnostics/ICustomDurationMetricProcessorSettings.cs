using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public interface ICustomDurationMetricProcessorSettings
{
    bool? ShouldRecord(Activity activity, Instrument instrument);

    Tags ExtractTags(Activity activity, Instrument instrument);
}
