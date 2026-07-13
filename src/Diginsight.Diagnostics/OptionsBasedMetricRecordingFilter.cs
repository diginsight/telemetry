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

        bool? HasMatches(IOptionsBasedMetricRecordingFilterOptions options)
        {
            IEnumerable<bool> matches = options
                .ActivityNames
                .Where(x => ActivityUtils.FullNameMatchesPattern(activitySourceName, activityName, x.Key))
                .Select(static x => x.Value);

            bool? result = null;
            foreach (bool match in matches)
            {
                if (!match)
                    return false;
                result = true;
            }
            return result;
        }

        return HasMatches(filterMonitor.Get(instrument.Name))
            ?? HasMatches(filterMonitor.CurrentValue)
            ?? false;
    }
}
