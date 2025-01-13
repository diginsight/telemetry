using System.Diagnostics;

namespace Diginsight.Diagnostics;

public interface IActivityLoggingSampler
{
    LogBehavior? GetLogBehavior(Activity activity);
}
