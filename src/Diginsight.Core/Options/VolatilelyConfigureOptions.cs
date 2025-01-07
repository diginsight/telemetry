using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using MseOptions = Microsoft.Extensions.Options.Options;

namespace Diginsight.Options;

public class VolatilelyConfigureOptions<TOptions>
    : IConfigureNamedOptions<TOptions>, IPostConfigureOptions<TOptions>, IOptionsChangeTokenSource<TOptions>
    where TOptions : class, IVolatilelyConfigurable
{
    private readonly IVolatileConfigurationStorage storage;

    public string? Name { get; }

    public VolatilelyConfigureOptions(
        string? name,
        IVolatileConfigurationStorageProvider storageProvider
    )
    {
        storage = storageProvider.Get(KnownVolatileConfigurationStorageNames.Configuration);
        Name = name;
    }

    public void Configure(TOptions options)
    {
        ConfigureCore(MseOptions.DefaultName, options);
    }

    public void Configure(string? name, TOptions options)
    {
        ConfigureCore(name ?? MseOptions.DefaultName, options);
    }

    public void PostConfigure(string? name, TOptions options)
    {
        ConfigureCore(name ?? MseOptions.DefaultName, options);
    }

    public IChangeToken GetChangeToken() => storage.Configuration.GetReloadToken();

    protected void ConfigureCore(string name, TOptions options, Func<IConfiguration, IConfiguration>? enrichConfiguration = null)
    {
        if (Name is not null && !string.Equals(Name, name, StringComparison.Ordinal))
            return;

        IConfiguration configuration = storage.Configuration;
        if (enrichConfiguration is not null)
        {
            configuration = enrichConfiguration(configuration);
        }
        configuration.Bind(options.MakeFiller());
    }
}
