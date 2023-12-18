using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public interface ICustomDurationMetricSampler
{
    bool? ShouldRecord(Activity activity, Instrument instrument);
}
