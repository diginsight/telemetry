using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace Diginsight.CAOptions;

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

    public void Apply(IEnumerable<KeyValuePair<string, string?>> entries)
    {
        if (!entries.Any())
        {
            return;
        }

        foreach ((string entryKey, string? entryValue) in entries)
        {
            configurationProvider.Set(entryKey, entryValue);
        }

        configurationRoot.Reload();
    }
}
