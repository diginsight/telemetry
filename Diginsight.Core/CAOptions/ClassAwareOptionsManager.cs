using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

public sealed class ClassAwareOptionsManager<TOptions, TClass> : IClassAwareOptionsSnapshot<TOptions, TClass>
    where TOptions : class
{
    private readonly IClassAwareOptionsFactory<TOptions> factory;
    private readonly IClassAwareOptionsCache<TOptions> cache = new ClassAwareOptionsCache<TOptions>();

    public TOptions Value => Get(Options.DefaultName);

    public ClassAwareOptionsManager(IClassAwareOptionsFactory<TOptions> factory)
    {
        this.factory = factory;
    }

    public TOptions Get(string? name)
    {
        name ??= Options.DefaultName;
        return cache.GetOrAdd(name, typeof(TClass), () => factory.Create(name, typeof(TClass)));
    }
}
