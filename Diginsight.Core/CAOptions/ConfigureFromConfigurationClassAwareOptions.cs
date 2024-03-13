using Microsoft.Extensions.Configuration;

namespace Diginsight.CAOptions;

public class ConfigureFromConfigurationClassAwareOptions<TOptions> : ConfigureClassAwareOptions<TOptions>
    where TOptions : class
{
    public ConfigureFromConfigurationClassAwareOptions(string? name, IConfiguration configuration, Action<BinderOptions>? configureBinder = null)
        : base(name, (@class, options) => Filter(configuration, @class).Bind(options, configureBinder)) { }

    private static IConfiguration Filter(IConfiguration configuration, Type @class)
    {
        return configuration switch
        {
            IFilteredConfiguration filtered =>
                filtered.Class == @class ? filtered : throw new ArgumentException("Configuration already filtered on another class"),
            IConfigurationRoot root =>
                (IConfiguration)Activator.CreateInstance(typeof(FilteredConfigurationRoot<>).MakeGenericType(@class), root)!,
            IConfigurationSection section =>
                (IConfiguration)Activator.CreateInstance(typeof(FilteredConfigurationSection<>).MakeGenericType(@class), section)!,
            _ =>
                (IConfiguration)Activator.CreateInstance(typeof(FilteredConfiguration<>).MakeGenericType(@class), configuration)!,
        };
    }
}
