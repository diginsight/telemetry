using Diginsight.CAOptions;
using System.Diagnostics;

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

    public bool? ShouldLog(Activity activity, Type? callerType)
    {
        IDiginsightActivityNamesOptions activityNamesOptions = activitiesOptions.Get(callerType ?? ClassAwareOptions.NoType).Freeze();

        return activity.NameMatchesPattern(activityNamesOptions.NonLoggedActivityNames) ? false
            : activity.NameMatchesPattern(activityNamesOptions.LoggedActivityNames) ? true
            : null;
    }

    public bool? ShouldRecord(Activity activity, Type? callerType)
    {
        IDiginsightActivityNamesOptions activityNamesOptions = activitiesOptions.Get(callerType ?? ClassAwareOptions.NoType).Freeze();

        return activity.NameMatchesPattern(activityNamesOptions.NonRecordedActivityNames) ? false
            : activity.NameMatchesPattern(activityNamesOptions.RecordedActivityNames) ? true
            : null;
    }
}
