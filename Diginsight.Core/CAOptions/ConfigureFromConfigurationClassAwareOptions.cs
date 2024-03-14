using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Diginsight.CAOptions;

public class ConfigureFromConfigurationClassAwareOptions<TOptions> : ConfigureClassAwareOptions<TOptions>
    where TOptions : class
{
    public ConfigureFromConfigurationClassAwareOptions(string? name, IConfiguration configuration, Action<BinderOptions>? configureBinder = null)
        : base(name, (@class, options) => Statics.Filter(configuration, @class).Bind(options, configureBinder)) { }
}

file static class Statics
{
    private static readonly MethodInfo FilterCore_Method = typeof(Statics).GetMethod(nameof(FilterCore), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static IConfiguration Filter(IConfiguration configuration, Type @class)
    {
        return (IConfiguration)FilterCore_Method.MakeGenericMethod(@class).Invoke(null, [ configuration ])!;
    }

    private static IConfiguration FilterCore<TClass>(IConfiguration configuration)
    {
        return configuration switch
        {
            IFilteredConfiguration filtered =>
                filtered.Class == typeof(TClass) ? filtered : throw new ArgumentException("Configuration already filtered on another class"),
            IConfigurationRoot root => new FilteredConfigurationRoot<TClass>(root),
            IConfigurationSection section => new FilteredConfigurationSection<TClass>(section),
            _ => new FilteredConfiguration<TClass>(configuration),
        };
    }
}
