using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace Diginsight.Options;

internal sealed class VolatileConfigurationStorage : IVolatileConfigurationStorage
{
    private readonly IConfigurationRoot configurationRoot;
    private readonly MemoryConfigurationProvider configurationProvider;

    public IConfiguration Configuration => configurationRoot;

    public VolatileConfigurationStorage()
    {
        configurationRoot = new ConfigurationBuilder().AddInMemoryCollection().Build();
        configurationProvider = configurationRoot.Providers.OfType<MemoryConfigurationProvider>().Single();
    }

    public void Apply(IEnumerable<KeyValuePair<string, string?>> entries, bool overwrite)
    {
        bool reload = false;

        if (overwrite)
        {
            reload = true;
            foreach (string entryKey in configurationProvider.Select(static x => x.Key))
            {
                configurationProvider.Set(entryKey, null);
            }
        }

        if (entries.Any())
        {
            reload = true;
            foreach ((string entryKey, string? entryValue) in entries)
            {
                configurationProvider.Set(entryKey, entryValue);
            }
        }

        if (reload)
        {
            configurationRoot.Reload();
        }
    }
}
