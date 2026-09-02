using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents a metric recording enricher based on configured activity tag names.
/// </summary>
public class OptionsBasedMetricRecordingEnricher : IMetricRecordingEnricher
{
    private readonly IOptionsMonitor<OptionsBasedMetricRecordingEnricherOptions> enricherMonitor;

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionsBasedMetricRecordingEnricher" /> class.
    /// </summary>
    /// <param name="enricherMonitor">The options monitor for <see cref="OptionsBasedMetricRecordingEnricherOptions" />.</param>
    /// <remarks>
    /// This class is designed to be either explicitly instantiated, instantiated through dependency injection, or derived.
    /// </remarks>
    public OptionsBasedMetricRecordingEnricher(
        IOptionsMonitor<OptionsBasedMetricRecordingEnricherOptions> enricherMonitor
    )
    {
        this.enricherMonitor = enricherMonitor;
    }

    /// <inheritdoc />
    public virtual Tags ExtractTags(Activity activity, Instrument instrument)
    {
        return ((IOptionsBasedMetricRecordingEnricherOptions)enricherMonitor.Get(instrument.Name)).MetricTags
            .Concat(((IOptionsBasedMetricRecordingEnricherOptions)enricherMonitor.CurrentValue).MetricTags)
            .Distinct()
            .Select(k => (Key: k, Value: activity.GetAncestors(true).Select(a => a.GetTagItem(k)).FirstOrDefault(static v => v is not null)))
            .Where(static x => x.Value is not null)
            .Select(static x => new Tag(x.Key, x.Value));
    }
}
