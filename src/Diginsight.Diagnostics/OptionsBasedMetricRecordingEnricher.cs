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
        static ICollection<string> GetTagNames(OptionsBasedMetricRecordingEnricherOptions options)
        {
            return ((IOptionsBasedMetricRecordingEnricherOptions)options).MetricTags;
        }

        return GetTagNames(enricherMonitor.Get(instrument.Name))
            .Concat(GetTagNames(enricherMonitor.CurrentValue))
            .Distinct()
            .Select(k => (Key: k, Value: activity.GetAncestors(true).Select(a => a.GetTagItem(k)).FirstOrDefault(static v => v is not null)))
            .Where(static x => x.Value is not null)
            .Select(static x => new Tag(x.Key, x.Value));
    }
}
