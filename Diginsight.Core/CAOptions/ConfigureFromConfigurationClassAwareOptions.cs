#if EXPERIMENT_FILTERED_CONFIGURATION
using Microsoft.Extensions.Configuration;

namespace Diginsight.CAOptions;

public class ConfigureFromConfigurationClassAwareOptions<TOptions> : ConfigureClassAwareOptions<TOptions>
    where TOptions : class
{
    public ConfigureFromConfigurationClassAwareOptions(string? name, IConfiguration configuration, Action<BinderOptions>? configureBinder = null)
        : base(name, (@class, options) => ConfigureCore(@class, options, configuration, configureBinder)) { }

    private static void ConfigureCore(Type @class, TOptions options, IConfiguration configuration, Action<BinderOptions>? configureBinder)
    {
        configuration = Filter(configuration, @class);
        configuration.Bind(options, configureBinder);
    }

    private static IConfiguration Filter(IConfiguration configuration, Type @class)
    {
        throw new NotImplementedException();
    }
}
#endif
