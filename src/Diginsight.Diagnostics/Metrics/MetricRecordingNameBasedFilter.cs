using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Diginsight.Diagnostics;

public class MetricRecordingNameBasedFilter : IMetricRecordingFilter
{
    private readonly MetricRecordingNameBasedFilterOptions filterOptions;

    public MetricRecordingNameBasedFilter(
        IOptionsMonitor<MetricRecordingNameBasedFilterOptions> filterOptions
    )
    {
        this.filterOptions = filterOptions.CurrentValue;
    }

    public virtual bool? ShouldRecord(Activity activity)
    {
        string activitySourceName = activity.Source.Name;
        string activityName = activity.OperationName;
        return filterOptions.ActivityNames
            .Where(x => ActivityUtils.FullNameMatchesPattern(activitySourceName, activityName, x.Key))
            .All(static x => x.Value);
    }
}
