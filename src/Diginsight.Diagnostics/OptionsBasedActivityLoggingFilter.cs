using Diginsight.Options;
using System.Diagnostics;

namespace Diginsight.Diagnostics;

public class OptionsBasedActivityLoggingFilter : IActivityLoggingFilter
{
    private readonly IClassAwareOptions<DiginsightActivitiesOptions> activitiesOptions;

    public OptionsBasedActivityLoggingFilter(
        IClassAwareOptions<DiginsightActivitiesOptions> activitiesOptions
    )
    {
        this.activitiesOptions = activitiesOptions;
    }

    public virtual LogBehavior? GetLogBehavior(Activity activity)
    {
        string activitySourceName = activity.Source.Name;
        string activityName = activity.OperationName;

        var activityNames = ((IDiginsightActivitiesLogOptions)activitiesOptions.Get(activity.GetCallerType()).Freeze()).ActivityNames;

        return activityNames.Where(x => ActivityUtils.FullNameMatchesPattern(activitySourceName, activityName, x.Key))
            .Select(static x => (LogBehavior?)x.Value)
            .Max();
    }
}
