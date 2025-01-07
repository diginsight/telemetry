#if !(NET || NETSTANDARD2_1_OR_GREATER)
using Microsoft.Extensions.Options;
#endif
using MseOptions = Microsoft.Extensions.Options.Options;

namespace Diginsight.Options;

internal sealed class UnnamedClassAwareOptionsManager<TOptions> : IClassAwareOptions<TOptions>
    where TOptions : class
{
    private readonly IClassAwareOptionsFactory<TOptions> factory;
    private readonly IClassAwareOptionsCache<TOptions> cache = new ClassAwareOptionsCache<TOptions>();

#if !(NET || NETSTANDARD2_1_OR_GREATER)
    TOptions IOptions<TOptions>.Value => Get(null);
#endif

    public UnnamedClassAwareOptionsManager(IClassAwareOptionsFactory<TOptions> factory)
    {
        this.factory = factory;
    }

    public TOptions Get(Type? @class)
    {
        @class ??= ClassAwareOptions.NoClass;
        return cache.TryGetValue(MseOptions.DefaultName, @class, out TOptions? options)
            ? options
            : cache.GetOrAdd(MseOptions.DefaultName, @class, static (n, c, f) => f.Create(n, c), factory);
    }
}
