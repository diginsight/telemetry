#if !(NETSTANDARD || NET8_0_OR_GREATER)
using System.Collections.Immutable;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class FrozenSet
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImmutableHashSet<T> ToFrozenSet<T>(
        this IEnumerable<T> source, IEqualityComparer<T>? comparer = null
    )
    {
        return source.ToImmutableHashSet(comparer);
    }
}
#endif
