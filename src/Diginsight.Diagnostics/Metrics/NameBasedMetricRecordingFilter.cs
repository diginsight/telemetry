using Diginsight.Options;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public class NameBasedMetricRecordingFilter : IMetricRecordingFilter
{
    private readonly IClassAwareOptionsMonitor<NameBasedMetricRecordingFilterOptions> filterMonitor;

    public NameBasedMetricRecordingFilter(
        IClassAwareOptionsMonitor<NameBasedMetricRecordingFilterOptions> filterMonitor
    )
    {
        this.filterMonitor = filterMonitor;
    }

    public virtual bool? ShouldRecord(Activity activity, Instrument instrument)
    {
        string activitySourceName = activity.Source.Name;
        string activityName = activity.OperationName;
        Type? callerType = activity.GetCallerType();

        IEnumerable<bool> GetMatches(NameBasedMetricRecordingFilterOptions options)
        {
            return ((INameBasedMetricRecordingFilterOptions)options.Freeze())
                .ActivityNames
                .Where(x => ActivityUtils.FullNameMatchesPattern(activitySourceName, activityName, x.Key))
                .Select(static x => x.Value);
        }

        IEnumerable<bool> specificMatches = GetMatches(filterMonitor.Get(instrument.Name, callerType));
        return specificMatches.Any()
            ? specificMatches.All(static x => x)
            : GetMatches(filterMonitor.Get(callerType)).All(static x => x);
    }
}
