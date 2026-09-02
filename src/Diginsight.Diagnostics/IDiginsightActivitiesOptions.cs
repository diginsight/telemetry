namespace Diginsight.Diagnostics;

/// <summary>
/// Represents activity source listener options.
/// </summary>
public interface IDiginsightActivitiesOptions
{
    /// <summary>
    /// Gets the activity source name patterns mapped to listener enablement values.
    /// </summary>
    IReadOnlyDictionary<string, bool> ActivitySources { get; }
}
