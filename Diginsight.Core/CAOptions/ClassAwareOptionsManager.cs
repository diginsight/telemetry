using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

public sealed class ClassAwareOptionsManager<TOptions> : IClassAwareOptionsSnapshot<TOptions>
    where TOptions : class
{
    private readonly IClassAwareOptionsFactory<TOptions> factory;
    private readonly IClassAwareOptionsCache<TOptions> cache = new ClassAwareOptionsCache<TOptions>();

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
    TOptions IOptions<TOptions>.Value => Get(null, null);
#endif

    public ClassAwareOptionsManager(IClassAwareOptionsFactory<TOptions> factory)
    {
        this.factory = factory;
    }

    public TOptions Get(string? name, Type? @class)
    {
        name ??= Options.DefaultName;
        @class ??= ClassAwareOptions.NoClass;

        return cache.TryGetValue(name, @class, out TOptions? options)
            ? options
            : cache.GetOrAdd(name, @class, static (n, c, f) => f.Create(n, c), factory);
    }

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
    TOptions IClassAwareOptions<TOptions>.Get(Type? @class) => Get(null, @class);

    TOptions IOptionsSnapshot<TOptions>.Get(string? name) => Get(name, null);
#endif
}
