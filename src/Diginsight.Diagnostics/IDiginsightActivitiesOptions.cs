namespace Diginsight.Diagnostics;

public interface IDiginsightActivitiesOptions
{
    IEnumerable<string> ActivitySources { get; }
    IEnumerable<string> NotActivitySources { get; }
}
