using Diginsight.Analyzers;

namespace Diginsight.Options;

/// <summary>
/// Applies volatile configuration to class-aware options, filtering the stored configuration for the requesting class.
/// </summary>
/// <typeparam name="TOptions">The type of options being configured.</typeparam>
[NonSealed]
public class VolatilelyConfigureClassAwareOptions<TOptions>
    : VolatilelyConfigureOptions<TOptions>,
        IConfigureClassAwareOptions<TOptions>,
        IPostConfigureClassAwareOptions<TOptions>,
        IClassAwareOptionsChangeTokenSource<TOptions>
    where TOptions : class, IVolatilelyConfigurable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VolatilelyConfigureClassAwareOptions{TOptions}" /> class.
    /// </summary>
    /// <param name="name">The name of the options to configure, or <c>null</c> to configure any name.</param>
    /// <param name="storageProvider">The provider of the volatile configuration storage.</param>
    public VolatilelyConfigureClassAwareOptions(
        string? name,
        IVolatileConfigurationStorageProvider storageProvider
    )
        : base(name, storageProvider) { }

    /// <inheritdoc />
    public void Configure(string name, Type @class, TOptions options)
    {
        ConfigureCore(name, options, configuration => FilteredConfiguration.For(configuration, @class));
    }

    /// <inheritdoc />
    public void PostConfigure(string name, Type @class, TOptions options)
    {
        ConfigureCore(name, options, configuration => FilteredConfiguration.For(configuration, @class));
    }
}
