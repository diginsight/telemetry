using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Diagnostics;

namespace Diginsight.Diagnostics;

public class MetricRecordingEnricherOptions
{
    public string MetricName { get; set; } = string.Empty;
    public ICollection<string> MetricTags { get; set; } = new List<string>();
}

public class MetricRecordingTagsEnricher : IMetricRecordingEnricher
{
    private readonly MetricRecordingEnricherOptions enricherOptions;

    public MetricRecordingTagsEnricher(
        IOptionsMonitor<MetricRecordingEnricherOptions> enricherOptions)
    {
        this.enricherOptions = enricherOptions.CurrentValue;
    }

    public IEnumerable<KeyValuePair<string, object?>> ExtractTags(Activity activity)
    {
        var tags = enricherOptions.MetricTags
                                  .Select(k => (Key: k, Value: activity.GetAncestors(true).Select(a => a.GetTagItem(k)).FirstOrDefault(static v => v is not null)))
                                  .Where(static x => x.Value is not null)
                                  .Select(static x => new KeyValuePair<string, object?>(x.Key, x.Value));

        return tags;
    }
}
