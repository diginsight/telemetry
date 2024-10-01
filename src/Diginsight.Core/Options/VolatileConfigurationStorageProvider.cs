using System.Collections.Concurrent;

namespace Diginsight.Options;

internal sealed class VolatileConfigurationStorageProvider : IVolatileConfigurationStorageProvider
{
    private readonly ConcurrentDictionary<string, IVolatileConfigurationStorage> storages = new ();

    public IVolatileConfigurationStorage Get(string name) => storages.GetOrAdd(name, static _ => new VolatileConfigurationStorage());
}
