using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public interface IMetricRecordingFilter
{
    bool? ShouldRecord(Activity activity, Instrument instrument);
}
