#if !NET6_0_OR_GREATER
using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace System.Collections.Generic;

public sealed class ReferenceEqualityComparer : IEqualityComparer<object?>, IEqualityComparer
{
    public static ReferenceEqualityComparer Instance { get; } = new ReferenceEqualityComparer();

    private ReferenceEqualityComparer() { }

    public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

    public int GetHashCode(object? obj) => RuntimeHelpers.GetHashCode(obj);
}
#endif
