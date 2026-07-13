using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public class OptionsBasedMetricRecordingEnricher : IMetricRecordingEnricher
{
    private readonly IOptionsMonitor<OptionsBasedMetricRecordingEnricherOptions> enricherMonitor;

    public OptionsBasedMetricRecordingEnricher(
        IOptionsMonitor<OptionsBasedMetricRecordingEnricherOptions> enricherMonitor
    )
    {
        this.enricherMonitor = enricherMonitor;
    }

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
