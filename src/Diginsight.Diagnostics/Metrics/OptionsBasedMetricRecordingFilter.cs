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
            return ((IOptionsBasedMetricRecordingFilterOptions)options.Freeze())
                .ActivityNames
                .Where(x => ActivityUtils.FullNameMatchesPattern(activitySourceName, activityName, x.Key))
                .Select(static x => x.Value);
        }

        IEnumerable<bool> specificMatches = GetMatches(filterMonitor.Get(instrument.Name));
        return specificMatches.Any()
            ? specificMatches.All(static x => x)
            : GetMatches(filterMonitor.CurrentValue).All(static x => x);
    }
}
