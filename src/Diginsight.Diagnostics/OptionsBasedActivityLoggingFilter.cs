using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Diginsight.Diagnostics;

public class OptionsBasedActivityLoggingFilter : IActivityLoggingFilter
{
    private readonly IOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor;

    public OptionsBasedActivityLoggingFilter(
        IOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor
    )
    {
        this.activitiesOptionsMonitor = activitiesOptionsMonitor;
    }

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
