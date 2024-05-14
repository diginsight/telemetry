using System.Diagnostics;

namespace Diginsight.Diagnostics;

public interface IActivityLoggingSampler
{
    bool? ShouldLog(Activity activity);
}
