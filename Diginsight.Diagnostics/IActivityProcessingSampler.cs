using System.Diagnostics;

namespace Diginsight.Diagnostics;

public interface IActivityProcessingSampler
{
    bool? ShouldLog(Activity activity, Type? callerType);

    bool? ShouldRecord(Activity activity, Type? callerType);
}
