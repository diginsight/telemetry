namespace Diginsight.CAOptions;

public class VolatilelyConfigureClassAwareOptions<TOptions>
    : VolatilelyConfigureOptions<TOptions>,
        IConfigureClassAwareOptions<TOptions>,
        IPostConfigureClassAwareOptions<TOptions>,
        IClassAwareOptionsChangeTokenSource<TOptions>
    where TOptions : class, IVolatilelyConfigurable
{
    public VolatilelyConfigureClassAwareOptions(
        string? name,
        IVolatileConfigurationStorage storage
    )
        : base(name, storage) { }

    public void Configure(string name, Type @class, TOptions options)
    {
        ConfigureCore(name, options, configuration => FilteredConfiguration.For(configuration, @class));
    }

    public void PostConfigure(string name, Type @class, TOptions options)
    {
        ConfigureCore(name, options, configuration => FilteredConfiguration.For(configuration, @class));
    }
}
