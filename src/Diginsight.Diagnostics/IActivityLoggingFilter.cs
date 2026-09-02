using System.Diagnostics;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents an interface for filtering activity lifecycle logging.
/// </summary>
public interface IActivityLoggingFilter
{
    /// <summary>
    /// Gets the activity lifecycle logging behavior for the specified activity.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The activity lifecycle logging behavior, or <c>null</c> if the filter does not decide, eventually falling back to the default behavior.</returns>
    LogBehavior? GetLogBehavior(Activity activity);
}
