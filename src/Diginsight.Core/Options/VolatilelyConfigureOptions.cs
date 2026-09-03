using Diginsight.Analyzers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using MseOptions = Microsoft.Extensions.Options.Options;

namespace Diginsight.Options;

/// <summary>
/// Applies volatile configuration to options that support runtime reconfiguration through <see cref="IVolatilelyConfigurable" />.
/// </summary>
/// <typeparam name="TOptions">The type of options being configured.</typeparam>
[NonSealed]
public class VolatilelyConfigureOptions<TOptions>
    : IConfigureNamedOptions<TOptions>, IPostConfigureOptions<TOptions>, IOptionsChangeTokenSource<TOptions>
    where TOptions : class, IVolatilelyConfigurable
{
    private readonly IVolatileConfigurationStorage storage;

    /// <inheritdoc />
    public string? Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VolatilelyConfigureOptions{TOptions}" /> class.
    /// </summary>
    /// <param name="name">The name of the options to configure, or <c>null</c> to configure any name.</param>
    /// <param name="storageProvider">The provider of the volatile configuration storage.</param>
    public VolatilelyConfigureOptions(
        string? name,
        IVolatileConfigurationStorageProvider storageProvider
    )
    {
        storage = storageProvider.Get(KnownVolatileConfigurationStorageNames.Configuration);
        Name = name;
    }

    /// <inheritdoc />
    public void Configure(TOptions options)
    {
        ConfigureCore(MseOptions.DefaultName, options);
    }

    /// <inheritdoc />
    public void Configure(string? name, TOptions options)
    {
        ConfigureCore(name ?? MseOptions.DefaultName, options);
    }

    /// <inheritdoc />
    public void PostConfigure(string? name, TOptions options)
    {
        ConfigureCore(name ?? MseOptions.DefaultName, options);
    }

    /// <inheritdoc />
    public IChangeToken GetChangeToken() => storage.Configuration.GetReloadToken();

    /// <summary>
    /// Binds the current volatile configuration onto the filler of the specified options instance.
    /// </summary>
    /// <param name="name">The name of the options being configured.</param>
    /// <param name="options">The options instance to configure.</param>
    /// <param name="enrichConfiguration">An optional transformation applied to the configuration before binding.</param>
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
