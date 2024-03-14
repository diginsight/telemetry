using Microsoft.Extensions.Configuration;

namespace Diginsight.CAOptions;

public sealed class ConfigureFromConfigurationClassAwareOptions<TOptions> : ConfigureClassAwareOptions<TOptions>
    where TOptions : class
{
    public ConfigureFromConfigurationClassAwareOptions(string? name, IConfiguration configuration, Action<BinderOptions>? configureBinder = null)
        : base(name, (@class, options) => FilteredConfiguration.For(configuration, @class).Bind(options, configureBinder)) { }
}
