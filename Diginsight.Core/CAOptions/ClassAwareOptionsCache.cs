using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Diginsight.CAOptions;

public sealed class ClassAwareOptionsCache<TOptions> : IClassAwareOptionsCache<TOptions>
    where TOptions : class
{
    private readonly ConcurrentDictionary<(string, Type?), Lazy<TOptions>> dict = new (new TupleEqualityComparer<string, Type?>(c1: StringComparer.Ordinal));

    public TOptions GetOrAdd(string? name, Type? @class, Func<TOptions> createOptions)
    {
        return dict.GetOrAdd(
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            (name ?? Options.DefaultName, @class), static (_, f) => new Lazy<TOptions>(f), createOptions
#else
            (name ?? Options.DefaultName, @class), _ => new Lazy<TOptions>(createOptions)
#endif
        ).Value;
    }

    public bool TryAdd(string? name, Type? @class, TOptions options)
    {
        return dict.TryAdd(
            (name ?? Options.DefaultName, @class),
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            new Lazy<TOptions>(options)
#else
            new Lazy<TOptions>(() => options)
#endif
        );
    }

    public bool TryRemove(string? name, Type? @class)
    {
        return dict.TryRemove((name ?? Options.DefaultName, @class), out _);
    }

    public void Clear()
    {
        dict.Clear();
    }
}
