using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using MseOptions = Microsoft.Extensions.Options.Options;

namespace Diginsight.Options;

public class DynamicallyConfigureOptions<TOptions> : IConfigureNamedOptions<TOptions>, IPostConfigureOptions<TOptions>
    where TOptions : class, IDynamicallyConfigurable
{
    private readonly string? name;
    private readonly IDynamicConfigurationLoader? dynamicConfigurationloader;

    public DynamicallyConfigureOptions(
        string? name,
        IDynamicConfigurationLoader? dynamicConfigurationloader = null
    )
    {
        this.name = name;
        this.dynamicConfigurationloader = dynamicConfigurationloader;
    }

    public void Configure(TOptions options)
    {
        ConfigureCore(MseOptions.DefaultName, options);
    }

    public void Configure([SuppressMessage("ReSharper", "ParameterHidesMember")] string? name, TOptions options)
    {
        ConfigureCore(name ?? MseOptions.DefaultName, options);
    }

    public void PostConfigure([SuppressMessage("ReSharper", "ParameterHidesMember")] string? name, TOptions options)
    {
        ConfigureCore(name ?? MseOptions.DefaultName, options);
    }

    protected void ConfigureCore(
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        string name,
        TOptions options,
        Func<IConfiguration, IConfiguration>? enrichConfiguration = null
    )
    {
        if (dynamicConfigurationloader is null ||
            (this.name is not null && !string.Equals(this.name, name, StringComparison.Ordinal)))
        {
            return;
        }

        IEnumerable<KeyValuePair<string, string?>> specs = dynamicConfigurationloader.Load();
        if (!specs.Any())
        {
            return;
        }

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(specs).Build();
        if (enrichConfiguration is not null)
        {
            configuration = enrichConfiguration(configuration);
        }
        configuration.Bind(options.MakeFiller());
    }
}
