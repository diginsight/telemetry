using System.Diagnostics;

namespace Diginsight.Diagnostics;

public interface IActivityProcessingSampler
{
    bool? ShouldLog(Activity activity);

    bool? ShouldRecord(Activity activity);
}
