namespace Diginsight.Options;

/// <summary>
/// Represents an interface for loading dynamic configuration key/value pairs.
/// </summary>
public interface IDynamicConfigurationLoader
{
    /// <summary>
    /// Loads the dynamic configuration entries.
    /// </summary>
    /// <returns>The sequence of configuration key/value pairs.</returns>
    IEnumerable<KeyValuePair<string, string?>> Load();
}
