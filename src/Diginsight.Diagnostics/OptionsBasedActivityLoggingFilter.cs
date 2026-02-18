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

        LogBehavior? maxBehavior = null;
        foreach (var kvp in ((IDiginsightActivitiesLogOptions)activitiesOptions.Get(activity.GetCallerType())).ActivityNames)
        {
            if (ActivityUtils.FullNameMatchesPattern(activitySourceName, activityName, kvp.Key))
            {
                // Track maximum LogBehavior value
                if (maxBehavior == null || kvp.Value > maxBehavior.Value)
                {
                    maxBehavior = kvp.Value;
                }
            }
        }

        return maxBehavior;
    }
}
