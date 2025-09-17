namespace Diginsight.Diagnostics;

public class MetricRecordingEnricherOptions
{
    public string MetricName { get; set; } = "";
    public ICollection<string> MetricTags { get; set; } = new List<string>();
}
