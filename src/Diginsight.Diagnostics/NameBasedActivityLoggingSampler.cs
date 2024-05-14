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
        activityNamesOptions = activitiesOptions.Value;
    }

    public virtual bool? ShouldLog(Activity activity)
    {
        string activityName = activity.OperationName;

        return ActivityUtils.NameMatchesPattern(activityName, activityNamesOptions.NonLoggedActivityNames) ? false
            : ActivityUtils.NameMatchesPattern(activityName, activityNamesOptions.LoggedActivityNames) ? true
            : null;
    }
}
