using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public class OptionsBasedMetricRecordingFilter : IMetricRecordingFilter
{
    private readonly IOptionsMonitor<OptionsBasedMetricRecordingFilterOptions> filterMonitor;

    public OptionsBasedMetricRecordingFilter(
        IOptionsMonitor<OptionsBasedMetricRecordingFilterOptions> filterMonitor
    )
    {
        this.filterMonitor = filterMonitor;
    }

    public virtual bool? ShouldRecord(Activity activity, Instrument instrument)
    {
        string activitySourceName = activity.Source.Name;
        string activityName = activity.OperationName;

        var specificOptions = filterMonitor.Get(instrument.Name);
        bool? specificResult = HasMatches(specificOptions, activitySourceName, activityName);
        if (specificResult.HasValue)
            return specificResult;

        var defaultOptions = filterMonitor.CurrentValue;
        return HasMatches(defaultOptions, activitySourceName, activityName) ?? false;
    }

    private static bool? HasMatches(OptionsBasedMetricRecordingFilterOptions options, string activitySourceName, string activityName)
    {
        bool hasMatch = false;
        foreach (var kvp in options.ActivityNames)
        {
            if (ActivityUtils.FullNameMatchesPattern(activitySourceName, activityName, kvp.Key))
            {
                hasMatch = true;
                if (!kvp.Value) // Early exit if any match is false
                    return false;
            }
        }
        return hasMatch ? true : (bool?)null;
    }


}
