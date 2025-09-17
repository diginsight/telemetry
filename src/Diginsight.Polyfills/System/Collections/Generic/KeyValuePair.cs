#if !(NET || NETSTANDARD2_1_OR_GREATER)
using System.Runtime.CompilerServices;

namespace System.Collections.Generic;

public static class KeyValuePair
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value)
    {
        return new KeyValuePair<TKey, TValue>(key, value);
    }
}
#endif
