using System.Collections.Concurrent;

namespace Diginsight.CAOptions;

internal sealed class VolatileConfigurationStorageProvider : IVolatileConfigurationStorageProvider
{
    private readonly ConcurrentDictionary<string, IVolatileConfigurationStorage> storages = new ();

    public IVolatileConfigurationStorage this[string name] => storages.GetOrAdd(name, static _ => new VolatileConfigurationStorage());
}
