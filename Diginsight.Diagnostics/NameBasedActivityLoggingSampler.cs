using Diginsight.CAOptions;
using System.Diagnostics;

namespace Diginsight.Diagnostics;

public class NameBasedActivityLoggingSampler : IActivityLoggingSampler
{
    private readonly IClassAwareOptionsMonitor<DiginsightActivitiesOptions> activitiesOptions;

    public NameBasedActivityLoggingSampler(
        IClassAwareOptionsMonitor<DiginsightActivitiesOptions> activitiesOptions
    )
    {
        this.activitiesOptions = activitiesOptions;
    }

    public virtual bool? ShouldLog(Activity activity)
    {
        IDiginsightActivityNamesOptions activityNamesOptions = activitiesOptions.Get(activity.GetCallerType()).Freeze();
        string activityName = activity.OperationName;

        return ActivityUtils.NameMatchesPattern(activityName, activityNamesOptions.NonLoggedActivityNames) ? false
            : ActivityUtils.NameMatchesPattern(activityName, activityNamesOptions.LoggedActivityNames) ? true
            : null;
    }
}
