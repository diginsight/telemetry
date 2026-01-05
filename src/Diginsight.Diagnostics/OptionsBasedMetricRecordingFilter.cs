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

        IEnumerable<bool> GetMatches(OptionsBasedMetricRecordingFilterOptions options)
        {
            return ((IOptionsBasedMetricRecordingFilterOptions)options)
                .ActivityNames
                .Where(x => ActivityUtils.FullNameMatchesPattern(activitySourceName, activityName, x.Key))
                .Select(static x => x.Value)
                .ToArray();
        }

        IEnumerable<bool> specificMatches = GetMatches(filterMonitor.Get(instrument.Name));
        if (specificMatches.Any())
        {
            return specificMatches.All(static x => x);
        }

        IEnumerable<bool> generalMatches = GetMatches(filterMonitor.CurrentValue);
        return generalMatches.Any() && generalMatches.All(static x => x);
    }
}
