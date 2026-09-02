using Microsoft.Extensions.Configuration;

namespace Diginsight.Options;

/// <summary>
/// Configures class-aware options by binding them from a configuration filtered for the requesting class.
/// </summary>
/// <typeparam name="TOptions">The type of options being configured.</typeparam>
public sealed class ConfigureClassAwareOptionsFromConfiguration<TOptions> : ConfigureClassAwareOptions<TOptions>
    where TOptions : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigureClassAwareOptionsFromConfiguration{TOptions}" /> class.
    /// </summary>
    /// <param name="name">The name of the options to configure, or <c>null</c> for the default name.</param>
    /// <param name="configuration">The configuration to bind the options from.</param>
    /// <param name="sectionKey">The key of the configuration section to bind, or <c>null</c> to bind the root.</param>
    /// <param name="configureBinder">An optional action that configures the binder options.</param>
    public ConfigureClassAwareOptionsFromConfiguration(
        string? name, IConfiguration configuration, string? sectionKey = null, Action<BinderOptions>? configureBinder = null
    )
        : base(name, (@class, options) => FilterAndBind(@class, options, configuration, sectionKey, configureBinder)) { }

    private static void FilterAndBind(
        Type @class, TOptions options, IConfiguration configuration, string? sectionKey, Action<BinderOptions>? configureBinder
    )
    {
        configuration = FilteredConfiguration.For(configuration, @class);
        if (sectionKey is not null)
        {
            configuration = configuration.GetSection(sectionKey);
        }
        configuration.Bind(options, configureBinder);
    }
}
