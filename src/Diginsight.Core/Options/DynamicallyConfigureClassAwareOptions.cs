namespace Diginsight.Options;

public class DynamicallyConfigureClassAwareOptions<TOptions>
    : DynamicallyConfigureOptions<TOptions>, IConfigureClassAwareOptions<TOptions>, IPostConfigureClassAwareOptions<TOptions>
    where TOptions : class, IDynamicallyConfigurable
{
    public DynamicallyConfigureClassAwareOptions(
        string? name,
        IDynamicConfigurationLoader? dynamicConfigurationloader = null
    )
        : base(name, dynamicConfigurationloader) { }

    public void Configure(string name, Type @class, TOptions options)
    {
        ConfigureCore(name, options, configuration => FilteredConfiguration.For(configuration, @class));
    }

    public void PostConfigure(string name, Type @class, TOptions options)
    {
        ConfigureCore(name, options, configuration => FilteredConfiguration.For(configuration, @class));
    }
}
