using Microsoft.Extensions.Configuration;

namespace Diginsight.Options;

public sealed class ConfigureClassAwareOptionsFromConfiguration<TOptions> : ConfigureClassAwareOptions<TOptions>
    where TOptions : class
{
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
