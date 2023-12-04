namespace Diginsight.CAOptions;

public interface IClassAwareOptionsCache<TOptions>
    where TOptions : class
{
    TOptions GetOrAdd(string? name, Type? @class, Func<TOptions> createOptions);

    bool TryAdd(string? name, Type? @class, TOptions options);

    bool TryRemove(string? name, Type? @class);

    void Clear();
}
