using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using MseOptions = Microsoft.Extensions.Options.Options;

namespace Diginsight.Options;

/// <summary>
/// Applies dynamic configuration to options that support runtime reconfiguration through <see cref="IDynamicallyConfigurable" />.
/// </summary>
/// <typeparam name="TOptions">The type of options being configured.</typeparam>
public class DynamicallyConfigureOptions<TOptions> : IConfigureNamedOptions<TOptions>, IPostConfigureOptions<TOptions>
    where TOptions : class, IDynamicallyConfigurable
{
    private readonly string? name;
    private readonly IDynamicConfigurationLoader? dynamicConfigurationloader;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicallyConfigureOptions{TOptions}" /> class.
    /// </summary>
    /// <param name="name">The name of the options to configure, or <c>null</c> to configure any name.</param>
    /// <param name="dynamicConfigurationloader">The loader that supplies the dynamic configuration, or <c>null</c> to disable dynamic configuration.</param>
    public DynamicallyConfigureOptions(
        string? name,
        IDynamicConfigurationLoader? dynamicConfigurationloader = null
    )
    {
        this.name = name;
        this.dynamicConfigurationloader = dynamicConfigurationloader;
    }

    /// <inheritdoc />
    public void Configure(TOptions options)
    {
        ConfigureCore(MseOptions.DefaultName, options);
    }

    /// <inheritdoc />
    public void Configure([SuppressMessage("ReSharper", "ParameterHidesMember")] string? name, TOptions options)
    {
        ConfigureCore(name ?? MseOptions.DefaultName, options);
    }

    /// <inheritdoc />
    public void PostConfigure([SuppressMessage("ReSharper", "ParameterHidesMember")] string? name, TOptions options)
    {
        ConfigureCore(name ?? MseOptions.DefaultName, options);
    }

    /// <summary>
    /// Loads the dynamic configuration and binds it onto the filler of the specified options instance.
    /// </summary>
    /// <param name="name">The name of the options being configured.</param>
    /// <param name="options">The options instance to configure.</param>
    /// <param name="enrichConfiguration">An optional transformation applied to the loaded configuration before binding.</param>
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
