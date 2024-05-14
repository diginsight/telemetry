using System.Diagnostics.CodeAnalysis;

namespace Diginsight.CAOptions;

public interface IClassAwareOptionsCache<TOptions>
    where TOptions : class
{
    TOptions GetOrAdd(string name, Type @class, Func<string, Type, TOptions> create);

    TOptions GetOrAdd<TArg>(string name, Type @class, Func<string, Type, TArg, TOptions> create, TArg creatorArg);

    bool TryGetValue(string name, Type @class, [NotNullWhen(true)] out TOptions? options);

    bool TryAdd(string name, Type @class, TOptions options);

    bool TryRemove(string name, Type @class);

    IEnumerable<Type> TryRemove(string name);

    IEnumerable<(string Name, IEnumerable<Type> Classes)> Clear();
}
