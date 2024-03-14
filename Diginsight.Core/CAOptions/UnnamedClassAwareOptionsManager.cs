using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

internal sealed class UnnamedClassAwareOptionsManager<TOptions> : IClassAwareOptions<TOptions>
    where TOptions : class
{
    private readonly IClassAwareOptionsFactory<TOptions> factory;
    private readonly IClassAwareOptionsCache<TOptions> cache = new ClassAwareOptionsCache<TOptions>();

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
    TOptions IOptions<TOptions>.Value => Get(ClassAwareOptions.NoType);
#endif

    public UnnamedClassAwareOptionsManager(IClassAwareOptionsFactory<TOptions> factory)
    {
        this.factory = factory;
    }

    public TOptions Get(Type @class)
    {
        return cache.TryGetValue(Options.DefaultName, @class, out TOptions? options)
            ? options
            : cache.GetOrAdd(Options.DefaultName, @class, static (n, c, f) => f.Create(n, c), factory);
    }
}
