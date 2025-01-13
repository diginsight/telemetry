using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Diginsight.Diagnostics;

public class NameBasedSpanDurationMetricRecorderSettings : ISpanDurationMetricRecorderSettings
{
    private readonly IDiginsightActivityNamesOptions activityNamesOptions;

    public NameBasedSpanDurationMetricRecorderSettings(
        IOptions<DiginsightActivitiesOptions> activitiesOptions
    )
    {
        activityNamesOptions = activitiesOptions.Value.Freeze();
    }

    public virtual bool? ShouldRecord(Activity activity)
    {
        string activitySourceName = activity.Source.Name;
        string activityName = activity.OperationName;
        return activityNamesOptions.SpanMeasuredActivityNames
            .Where(x => ActivityUtils.FullNameMatchesPattern(activitySourceName, activityName, x.Key))
            .All(static x => x.Value);
    }

    public virtual Tags ExtractTags(Activity activity) => [ ];
}
