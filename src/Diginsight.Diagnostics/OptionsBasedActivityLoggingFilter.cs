using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Diginsight.Diagnostics;

public class OptionsBasedActivityLoggingFilter : IActivityLoggingFilter
{
    private readonly IOptions<DiginsightActivitiesOptions> activitiesOptions;

    private IDiginsightActivitiesLogOptions LogOptions =>
        field ??= activitiesOptions.Value.Freeze();

    public OptionsBasedActivityLoggingFilter(
        IOptions<DiginsightActivitiesOptions> activitiesOptions
    )
    {
        this.activitiesOptions = activitiesOptions;
    }

    public virtual LogBehavior? GetLogBehavior(Activity activity)
    {
        string activitySourceName = activity.Source.Name;
        string activityName = activity.OperationName;

        return LogOptions
            .ActivityNames
            .Where(x => ActivityUtils.FullNameMatchesPattern(activitySourceName, activityName, x.Key))
            .Select(static x => (LogBehavior?)x.Value)
            .Max();
    }
}
