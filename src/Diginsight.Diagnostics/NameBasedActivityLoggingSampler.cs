using Diginsight.Options;
using System.Diagnostics;

namespace Diginsight.Diagnostics;

public class NameBasedActivityLoggingSampler : IActivityLoggingSampler
{
    private readonly IClassAwareOptions<DiginsightActivitiesOptions> activitiesOptions;

    public NameBasedActivityLoggingSampler(
        IClassAwareOptions<DiginsightActivitiesOptions> activitiesOptions
    )
    {
        this.activitiesOptions = activitiesOptions;
    }

    public virtual LogBehavior? GetLogBehavior(Activity activity)
    {
        string activitySourceName = activity.Source.Name;
        string activityName = activity.OperationName;

        return activitiesOptions
            .Get(activity.GetCallerType())
            .Freeze()
            .LoggedActivityNames
            .Where(x => ActivityUtils.FullNameMatchesPattern(activitySourceName, activityName, x.Key))
            .Select(static x => (LogBehavior?)x.Value)
            .Max();
    }
}
