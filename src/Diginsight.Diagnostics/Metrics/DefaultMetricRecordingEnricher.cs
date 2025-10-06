using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Diginsight.Diagnostics;

public class DefaultMetricRecordingEnricher : IMetricRecordingEnricher
{
    private readonly IOptionsMonitor<DefaultMetricRecordingEnricherOptions> enricherMonitor;

    public DefaultMetricRecordingEnricher(
        IOptionsMonitor<DefaultMetricRecordingEnricherOptions> enricherMonitor
    )
    {
        this.enricherMonitor = enricherMonitor;
    }

    public virtual Tags ExtractTags(Activity activity, Instrument instrument)
    {
        static IReadOnlyCollection<string> GetTagNames(DefaultMetricRecordingEnricherOptions options)
        {
            return ((IDefaultMetricRecordingEnricherOptions)options.Freeze()).MetricTags;
        }

        IReadOnlyCollection<string> tagNames =
            GetTagNames(enricherMonitor.Get(instrument.Name)) is { Count: > 0 } specificTagNames
                ? specificTagNames
                : GetTagNames(enricherMonitor.CurrentValue);

        return tagNames
            .Select(k => (Key: k, Value: activity.GetAncestors(true).Select(a => a.GetTagItem(k)).FirstOrDefault(static v => v is not null)))
            .Where(static x => x.Value is not null)
            .Select(static x => new Tag(x.Key, x.Value));
    }
}
