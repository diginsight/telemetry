using Microsoft.Extensions.Configuration;

namespace Diginsight.Options;

/// <summary>
/// Represents a storage for volatile configuration settings.
/// </summary>
public interface IVolatileConfigurationStorage
{
    /// <summary>
    /// Gets the current volatile configuration.
    /// </summary>
    IConfiguration Configuration { get; }

    /// <summary>
    /// Applies the specified configuration entries.
    /// </summary>
    /// <param name="entries">The configuration entries to apply.</param>
    /// <param name="overwrite">If set to <c>true</c>, existing entries will be overwritten.</param>
    void Apply(IEnumerable<KeyValuePair<string, string?>> entries, bool overwrite = false);
}
