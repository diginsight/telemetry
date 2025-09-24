namespace Diginsight.Diagnostics;

public interface IOptionsBasedMetricRecordingFilterOptions
{
    IReadOnlyDictionary<string, bool> ActivityNames { get; }
}
