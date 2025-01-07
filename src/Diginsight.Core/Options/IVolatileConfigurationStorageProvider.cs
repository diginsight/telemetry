namespace Diginsight.Options;

/// <summary>
/// Represents a provider for <see cref="IVolatileConfigurationStorage" /> instances.
/// </summary>
public interface IVolatileConfigurationStorageProvider
{
    /// <summary>
    /// Gets (or creates) a <see cref="IVolatileConfigurationStorage" /> instance by name.
    /// </summary>
    /// <param name="name">The name of the configuration storage.</param>
    /// <returns>The <see cref="IVolatileConfigurationStorage" /> instance associated with <paramref name="name" />.</returns>
    IVolatileConfigurationStorage Get(string name);
}
