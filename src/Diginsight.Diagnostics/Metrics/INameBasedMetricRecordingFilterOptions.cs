namespace Diginsight.Diagnostics;

public interface INameBasedMetricRecordingFilterOptions
{
    IReadOnlyDictionary<string, bool> ActivityNames { get; }
}
