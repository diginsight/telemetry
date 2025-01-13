namespace Diginsight.Diagnostics;

public interface IDiginsightActivitiesOptions
{
    IReadOnlyDictionary<string, bool> ActivitySources { get; }
}
