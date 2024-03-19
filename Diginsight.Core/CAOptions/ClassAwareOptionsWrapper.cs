using System.Collections.Immutable;

namespace Diginsight.CAOptions;

public sealed class ClassAwareOptionsWrapper<TOptions> : IClassAwareOptions<TOptions>
    where TOptions : class
{
    private readonly IReadOnlyDictionary<Type, TOptions> valuesByType;

    public TOptions Value { get; }

    public ClassAwareOptionsWrapper(TOptions defaultValue, IReadOnlyDictionary<Type, TOptions>? valuesByType = null)
    {
        this.valuesByType = valuesByType ?? ImmutableDictionary<Type, TOptions>.Empty;
        Value = defaultValue;
    }

    public TOptions Get(Type @class)
    {
        return @class != ClassAwareOptions.NoType && valuesByType.TryGetValue(@class, out TOptions? value) ? value : Value;
    }
}
