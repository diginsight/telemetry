namespace Diginsight;

public sealed class OptionsCacheSettings
{
    public ISet<(Type, string?)> DynamicEntries { get; } = new HashSet<(Type, string?)>(new TupleEqualityComparer<Type, string?>(c2: StringComparer.Ordinal));
}
