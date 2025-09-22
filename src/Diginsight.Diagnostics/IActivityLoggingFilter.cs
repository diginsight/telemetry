using System.Diagnostics;

namespace Diginsight.Diagnostics;

public interface IActivityLoggingFilter
{
    LogBehavior? GetLogBehavior(Activity activity);
}
