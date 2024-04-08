using Diginsight.CAOptions;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
        IDiginsightActivityNamesOptions activityNamesOptions = GetActivityNamesOptions(activity);

        return activity.NameMatchesPattern(activityNamesOptions.NonLoggedActivityNames) ? false
            : activity.NameMatchesPattern(activityNamesOptions.LoggedActivityNames) ? true
            : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DiginsightActivitiesOptions GetActivityNamesOptions(Activity activity)
    {
        return activitiesOptions.Get(activity.GetCallerType()).Freeze();
    }
}
