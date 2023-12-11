using System.Diagnostics;

namespace Diginsight.Diagnostics;

public interface IActivityRecordingSampler
{
    void ShouldRecord(Activity activity, ref bool? result);
}
