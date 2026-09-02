namespace Diginsight.Diagnostics;

/// <summary>
/// Specifies activity lifecycle logging behavior.
/// </summary>
public enum LogBehavior
{
    /// <summary>
    /// Shows activity lifecycle logs for the activity.
    /// </summary>
    Show,
    /// <summary>
    /// Hides activity lifecycle logs for the activity.
    /// </summary>
    Hide,
    /// <summary>
    /// Hides activity lifecycle logs for the activity and all of its descendants (cannot be overridden).
    /// </summary>
    Truncate,
}
