#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace System;

internal readonly struct Range : IEquatable<Range>
{
    public Index Start { get; }

    public Index End { get; }

    public Range(Index start, Index end)
    {
        Start = start;
        End = end;
    }

    public override bool Equals(object? obj)
    {
        return obj is Range range && range.Start.Equals(Start) && range.End.Equals(End);
    }

    public bool Equals(Range other) => other.Start.Equals(Start) && other.End.Equals(End);

    public override int GetHashCode()
    {
        return HashCode.Combine(Start.GetHashCode(), End.GetHashCode());
    }

    public static Range StartAt(Index start) => new Range(start, Index.End);

    public static Range EndAt(Index end) => new Range(Index.Start, end);

    public static Range All => new Range(Index.Start, Index.End);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (int Offset, int Length) GetOffsetAndLength(int length)
    {
        int start = Start.GetOffset(length);
        int end = End.GetOffset(length);

        if ((uint)end > (uint)length || (uint)start > (uint)end)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        return (start, end - start);
    }

    public override string ToString() => $"{Start}..{End}";
}
#endif
