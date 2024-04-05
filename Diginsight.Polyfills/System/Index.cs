#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
using System.Runtime.CompilerServices;

namespace System;

public readonly struct Index : IEquatable<Index>
{
    private readonly int value;

    public Index(int value, bool fromEnd = false)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value));
        this.value = fromEnd ? ~value : value;
    }

    private Index(int value)
    {
        this.value = value;
    }

    public static Index Start => new Index(0);

    public static Index End => new Index(~0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Index FromStart(int value)
    {
        return new Index(value < 0 ? throw new ArgumentOutOfRangeException(nameof(value)) : value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Index FromEnd(int value)
    {
        return new Index(value < 0 ? throw new ArgumentOutOfRangeException(nameof(value)) : ~value);
    }

    public int Value => value < 0 ? ~value : value;

    public bool IsFromEnd => value < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetOffset(int length)
    {
        int offset = value;
        if (IsFromEnd)
        {
            offset += length + 1;
        }
        return offset;
    }

    public override bool Equals(object? obj) => obj is Index index && value == index.value;

    public bool Equals(Index other) => value == other.value;

    public override int GetHashCode() => value;

    public override string ToString() => $"{(IsFromEnd ? "^" : "")}{(uint)Value}";

    public static implicit operator Index(int value) => FromStart(value);
}
#endif
