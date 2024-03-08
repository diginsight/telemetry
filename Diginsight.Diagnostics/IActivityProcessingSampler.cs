using System.Diagnostics;

namespace Diginsight.Diagnostics;

public interface IActivityProcessingSampler
{
    void ShouldLog(Activity activity, Type? callerType, ref bool? result);

    void ShouldRecord(Activity activity, Type? callerType, ref bool? result);
}
