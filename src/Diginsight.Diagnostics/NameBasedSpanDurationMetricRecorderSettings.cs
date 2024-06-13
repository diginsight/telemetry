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
        return ActivityUtils.FullNameCompliesWithPatterns(
            activity.Source.Name, activity.OperationName, activityNamesOptions.SpanMeasuredActivityNames, activityNamesOptions.NonSpanMeasuredActivityNames
        );
    }

    public virtual Tags ExtractTags(Activity activity) => [ ];
}
