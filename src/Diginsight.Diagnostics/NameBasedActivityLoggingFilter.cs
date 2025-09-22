using Diginsight.Options;
using System.Diagnostics;

namespace Diginsight.Diagnostics;

public class NameBasedActivityLoggingFilter : IActivityLoggingFilter
{
    private readonly IClassAwareOptions<DiginsightActivitiesOptions> activitiesOptions;

    public NameBasedActivityLoggingFilter(
        IClassAwareOptions<DiginsightActivitiesOptions> activitiesOptions
    )
    {
        this.activitiesOptions = activitiesOptions;
    }

    public virtual LogBehavior? GetLogBehavior(Activity activity)
    {
        string activitySourceName = activity.Source.Name;
        string activityName = activity.OperationName;

        return ((IDiginsightActivitiesLogOptions)activitiesOptions
                .Get(activity.GetCallerType())
                .Freeze())
            .ActivityNames
            .Where(x => ActivityUtils.FullNameMatchesPattern(activitySourceName, activityName, x.Key))
            .Select(static x => (LogBehavior?)x.Value)
            .Max();
    }
}
