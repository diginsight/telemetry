using System.Diagnostics;

namespace Diginsight.Diagnostics;

public interface ISpanDurationMetricSampler
{
    bool ShouldRecord(Activity activity);
}
