#if !NET
using System.Runtime.CompilerServices;

namespace System.Collections.Generic;

public sealed class ReferenceEqualityComparer : IEqualityComparer<object?>, IEqualityComparer
{
    public static ReferenceEqualityComparer Instance { get; } = new ();

    private ReferenceEqualityComparer() { }

    public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

    public int GetHashCode(object? obj) => RuntimeHelpers.GetHashCode(obj);
}
#endif
