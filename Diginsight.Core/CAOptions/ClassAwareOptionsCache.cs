﻿using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Diginsight.CAOptions;

public sealed class ClassAwareOptionsCache<TOptions> : IClassAwareOptionsCache<TOptions>
    where TOptions : class
{
    private readonly ConcurrentDictionary<(string, Type?), Lazy<TOptions>> dict = new (new TupleEqualityComparer<string, Type?>(c1: StringComparer.Ordinal));

    public TOptions GetOrAdd(string name, Type? @class, Func<string, Type?, TOptions> create)
    {
        return dict.GetOrAdd(
            (name, @class),
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            static ((string Name, Type? Class) k, Func<string, Type?, TOptions> a) =>
                new Lazy<TOptions>(() => a(k.Name, k.Class)),
            create
#else
            ((string Name, Type? Class) k) => new Lazy<TOptions>(() => create(k.Name, k.Class))
#endif
        ).Value;
    }

    public TOptions GetOrAdd<TArg>(string name, Type? @class, Func<string, Type?, TArg, TOptions> create, TArg creatorArg)
    {
        return dict.GetOrAdd(
            (name, @class),
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            static ((string Name, Type? Class) k, (Func<string, Type?, TArg, TOptions> Create, TArg Arg) a) =>
                new Lazy<TOptions>(() => a.Create(k.Name, k.Class, a.Arg)),
            (create, creatorArg)
#else
            ((string Name, Type? Class) k) => new Lazy<TOptions>(() => create(k.Name, k.Class, creatorArg))
#endif
        ).Value;
    }

    public bool TryGetValue(string name, Type? @class, [NotNullWhen(true)] out TOptions? options)
    {
        options = dict.TryGetValue((name, @class), out Lazy<TOptions>? lazy) ? lazy.Value : null;
        return options is not null;
    }

    public bool TryAdd(string name, Type? @class, TOptions options)
    {
        return dict.TryAdd(
            (name, @class),
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            new Lazy<TOptions>(options)
#else
            new Lazy<TOptions>(() => options)
#endif
        );
    }

    public bool TryRemove(string name, Type? @class)
    {
        return dict.TryRemove((name, @class), out _);
    }

    public void Clear()
    {
        dict.Clear();
    }
}