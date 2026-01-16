#if !(NETSTANDARD || NET8_0_OR_GREATER)
using System.Collections.Immutable;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class FrozenDictionary
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImmutableDictionary<TKey, TValue> ToFrozenDictionary<TKey, TValue>(
        this IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey>? comparer = null
    )
        where TKey : notnull
    {
        return source.ToImmutableDictionary(comparer);
    }
}

public static class FrozenDictionary<TKey, TValue>
    where TKey : notnull
{
    public static ImmutableDictionary<TKey, TValue> Empty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ImmutableDictionary<TKey, TValue>.Empty;
    }
}
#endif
