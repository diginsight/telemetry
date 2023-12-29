using System.Diagnostics;

namespace Diginsight.Diagnostics;

public interface IActivityRecordingSampler
{
    void ShouldRecord(Activity activity, Type? callerType, ref bool? result);
}
