using Diginsight.Analyzers;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents a metric recording filter based on configured activity name patterns.
/// </summary>
[NonSealed]
public class OptionsBasedMetricRecordingFilter : IMetricRecordingFilter
{
    private readonly IOptionsMonitor<OptionsBasedMetricRecordingFilterOptions> filterMonitor;

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionsBasedMetricRecordingFilter" /> class.
    /// </summary>
    /// <param name="filterMonitor">The options monitor for <see cref="OptionsBasedMetricRecordingFilterOptions" />.</param>
    /// <remarks>
    /// This class is designed to be either explicitly instantiated, instantiated through dependency injection, or derived.
    /// </remarks>
    public OptionsBasedMetricRecordingFilter(
        IOptionsMonitor<OptionsBasedMetricRecordingFilterOptions> filterMonitor
    )
    {
        this.filterMonitor = filterMonitor;
    }

    /// <inheritdoc />
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
