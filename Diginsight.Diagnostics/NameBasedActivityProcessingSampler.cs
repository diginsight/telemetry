using Diginsight.CAOptions;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public class NameBasedActivityProcessingSampler : IActivityProcessingSampler
{
    private readonly IClassAwareOptionsMonitor<DiginsightActivitiesOptions> activitiesOptions;

    public NameBasedActivityProcessingSampler(
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

    public virtual bool? ShouldRecord(Activity activity)
    {
        IDiginsightActivityNamesOptions activityNamesOptions = GetActivityNamesOptions(activity);

        return activity.NameMatchesPattern(activityNamesOptions.NonRecordedActivityNames) ? false
            : activity.NameMatchesPattern(activityNamesOptions.RecordedActivityNames) ? true
            : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DiginsightActivitiesOptions GetActivityNamesOptions(Activity activity)
    {
        return activitiesOptions.Get(activity.GetCallerType()).Freeze();
    }
}
