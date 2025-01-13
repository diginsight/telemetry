using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Diginsight.Diagnostics;

public class NameBasedActivityLoggingSampler : IActivityLoggingSampler
{
    private readonly IDiginsightActivityNamesOptions activityNamesOptions;

    public NameBasedActivityLoggingSampler(
        IOptions<DiginsightActivitiesOptions> activitiesOptions
    )
    {
        activityNamesOptions = activitiesOptions.Value.Freeze();
    }

    public virtual LogBehavior? GetLogBehavior(Activity activity)
    {
        string activitySourceName = activity.Source.Name;
        string activityName = activity.OperationName;
        return activityNamesOptions.LoggedActivityNames
            .Where(x => ActivityUtils.FullNameMatchesPattern(activitySourceName, activityName, x.Key))
            .Select(static x => (LogBehavior?)x.Value)
            .Max();
    }
}
