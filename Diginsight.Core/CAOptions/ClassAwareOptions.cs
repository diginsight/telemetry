using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

public static class ClassAwareOptions
{
    public static readonly Type NoType = typeof(ClassAwareOptions);

    public static TOptions Create<TOptions>(this IClassAwareOptionsFactory<TOptions> optionsFactory, Type @class)
        where TOptions : class
    {
        return optionsFactory.Create(Options.DefaultName, @class);
    }

    public static TOptions Get<TOptions>(this IClassAwareOptionsMonitor<TOptions> optionsMonitor, Type @class)
        where TOptions : class
    {
        return optionsMonitor.Get(Options.DefaultName, @class);
    }
}
