using System.Diagnostics;

namespace Diginsight.Diagnostics;

public interface IMetricRecordingFilter
{
    bool? ShouldRecord(Activity activity);
}
