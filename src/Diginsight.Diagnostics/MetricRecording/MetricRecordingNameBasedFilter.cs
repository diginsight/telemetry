using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Diginsight.Diagnostics;

public class MetricRecordingNameBasedFilterOptions
{
    public string MetricName { get; set; } = string.Empty;
    public IDictionary<string, bool> ActivityNames { get; set; } = new Dictionary<string, bool>();
}

public class MetricRecordingNameBasedFilter : IMetricRecordingFilter
{
    private readonly MetricRecordingNameBasedFilterOptions filterOptions;

    public MetricRecordingNameBasedFilter(
        MetricRecordingNameBasedFilterOptions filterOptions
    )
    {
        this.filterOptions = filterOptions;
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
