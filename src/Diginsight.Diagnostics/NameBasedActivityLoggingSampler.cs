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

    public virtual bool? ShouldLog(Activity activity)
    {
        return ActivityUtils.FullNameCompliesWithPatterns(
            activity.Source.Name, activity.OperationName, activityNamesOptions.LoggedActivityNames, activityNamesOptions.NonLoggedActivityNames
        );
    }
}
