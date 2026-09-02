using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents an activity lifecycle logging filter based on activity options.
/// </summary>
public class OptionsBasedActivityLoggingFilter : IActivityLoggingFilter
{
    private readonly IOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor;

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionsBasedActivityLoggingFilter" /> class.
    /// </summary>
    /// <param name="activitiesOptionsMonitor">The options monitor for <see cref="DiginsightActivitiesOptions" />.</param>
    /// <remarks>
    /// This class is designed to be either explicitly instantiated, instantiated through dependency injection, or derived.
    /// </remarks>
    public OptionsBasedActivityLoggingFilter(
        IOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor
    )
    {
        this.activitiesOptionsMonitor = activitiesOptionsMonitor;
    }

    /// <inheritdoc />
    public virtual LogBehavior? GetLogBehavior(Activity activity)
    {
        string activitySourceName = activity.Source.Name;
        string activityName = activity.OperationName;

        return ((IDiginsightActivitiesLogOptions)activitiesOptionsMonitor.CurrentValue)
            .ActivityNames
            .Where(x => ActivityUtils.FullNameMatchesPattern(activitySourceName, activityName, x.Key))
            .Select(static x => (LogBehavior?)x.Value)
            .Max();
    }
}
