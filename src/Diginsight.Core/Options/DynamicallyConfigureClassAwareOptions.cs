namespace Diginsight.Options;

/// <summary>
/// Applies dynamic configuration to class-aware options, filtering the loaded configuration for the requesting class.
/// </summary>
/// <typeparam name="TOptions">The type of options being configured.</typeparam>
public class DynamicallyConfigureClassAwareOptions<TOptions>
    : DynamicallyConfigureOptions<TOptions>, IConfigureClassAwareOptions<TOptions>, IPostConfigureClassAwareOptions<TOptions>
    where TOptions : class, IDynamicallyConfigurable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicallyConfigureClassAwareOptions{TOptions}" /> class.
    /// </summary>
    /// <param name="name">The name of the options to configure, or <c>null</c> to configure any name.</param>
    /// <param name="dynamicConfigurationloader">The loader that supplies the dynamic configuration, or <c>null</c> to disable dynamic configuration.</param>
    public DynamicallyConfigureClassAwareOptions(
        string? name,
        IDynamicConfigurationLoader? dynamicConfigurationloader = null
    )
        : base(name, dynamicConfigurationloader) { }

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
