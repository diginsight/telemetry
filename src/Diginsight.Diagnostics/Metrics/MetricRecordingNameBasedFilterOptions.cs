namespace Diginsight.Diagnostics;

public class MetricRecordingNameBasedFilterOptions
{
    public string MetricName { get; set; } = string.Empty;
    public IDictionary<string, bool> ActivityNames { get; set; } = new Dictionary<string, bool>();
}
