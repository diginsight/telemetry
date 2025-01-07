namespace Diginsight.Options;

public class VolatilelyConfigureClassAwareOptions<TOptions>
    : VolatilelyConfigureOptions<TOptions>,
        IConfigureClassAwareOptions<TOptions>,
        IPostConfigureClassAwareOptions<TOptions>,
        IClassAwareOptionsChangeTokenSource<TOptions>
    where TOptions : class, IVolatilelyConfigurable
{
    public VolatilelyConfigureClassAwareOptions(
        string? name,
        IVolatileConfigurationStorageProvider storageProvider
    )
        : base(name, storageProvider) { }

    public void Configure(string name, Type @class, TOptions options)
    {
        ConfigureCore(name, options, configuration => FilteredConfiguration.For(configuration, @class));
    }

    public void PostConfigure(string name, Type @class, TOptions options)
    {
        ConfigureCore(name, options, configuration => FilteredConfiguration.For(configuration, @class));
    }
}
