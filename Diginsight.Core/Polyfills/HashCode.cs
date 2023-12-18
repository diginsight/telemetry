#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace System;

public struct HashCode
{
    private const string HashCodeMutable = "HashCode is a mutable struct and should not be compared with other HashCodes.";

    private static readonly uint seed = GenerateGlobalSeed();

    private uint v1;
    private uint v2;
    private uint v3;
    private uint v4;
    private uint queue1;
    private uint queue2;
    private uint queue3;
    private uint length;

    private static uint GenerateGlobalSeed()
    {
        byte[] buffer = new byte[4];
        new Random().NextBytes(buffer);
        return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
    }

    public static int Combine<T1>(T1 value1)
    {
        uint hashCode = value1 is not null ? (uint)value1.GetHashCode() : 0U;
        return (int)MixFinal(QueueRound(MixEmptyState() + 4U, hashCode));
    }

    public static int Combine<T1, T2>(T1 value1, T2 value2)
    {
        uint hashCode1 = value1 is not null ? (uint)value1.GetHashCode() : 0U;
        uint hashCode2 = value2 is not null ? (uint)value2.GetHashCode() : 0U;
        return (int)MixFinal(QueueRound(QueueRound(MixEmptyState() + 8U, hashCode1), hashCode2));
    }

    public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
    {
        uint hashCode1 = value1 is not null ? (uint)value1.GetHashCode() : 0U;
        uint hashCode2 = value2 is not null ? (uint)value2.GetHashCode() : 0U;
        uint hashCode3 = value3 is not null ? (uint)value3.GetHashCode() : 0U;
        return (int)MixFinal(QueueRound(QueueRound(QueueRound(MixEmptyState() + 12U, hashCode1), hashCode2), hashCode3));
    }

    public static int Combine<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
    {
        uint hashCode1 = value1 is not null ? (uint)value1.GetHashCode() : 0U;
        uint hashCode2 = value2 is not null ? (uint)value2.GetHashCode() : 0U;
        uint hashCode3 = value3 is not null ? (uint)value3.GetHashCode() : 0U;
        uint hashCode4 = value4 is not null ? (uint)value4.GetHashCode() : 0U;
        Initialize(out uint v1, out uint v2, out uint v3, out uint v4);
        v1 = Round(v1, hashCode1);
        v2 = Round(v2, hashCode2);
        v3 = Round(v3, hashCode3);
        v4 = Round(v4, hashCode4);
        return (int)MixFinal(MixState(v1, v2, v3, v4) + 16U);
    }

    public static int Combine<T1, T2, T3, T4, T5>(
        T1 value1,
        T2 value2,
        T3 value3,
        T4 value4,
        T5 value5
    )
    {
        uint hashCode1 = value1 is not null ? (uint)value1.GetHashCode() : 0U;
        uint hashCode2 = value2 is not null ? (uint)value2.GetHashCode() : 0U;
        uint hashCode3 = value3 is not null ? (uint)value3.GetHashCode() : 0U;
        uint hashCode4 = value4 is not null ? (uint)value4.GetHashCode() : 0U;
        uint hashCode5 = value5 is not null ? (uint)value5.GetHashCode() : 0U;
        Initialize(out uint v1, out uint v2, out uint v3, out uint v4);
        v1 = Round(v1, hashCode1);
        v2 = Round(v2, hashCode2);
        v3 = Round(v3, hashCode3);
        v4 = Round(v4, hashCode4);
        return (int)MixFinal(QueueRound(MixState(v1, v2, v3, v4) + 20U, hashCode5));
    }

    public static int Combine<T1, T2, T3, T4, T5, T6>(
        T1 value1,
        T2 value2,
        T3 value3,
        T4 value4,
        T5 value5,
        T6 value6
    )
    {
        uint hashCode1 = value1 is not null ? (uint)value1.GetHashCode() : 0U;
        uint hashCode2 = value2 is not null ? (uint)value2.GetHashCode() : 0U;
        uint hashCode3 = value3 is not null ? (uint)value3.GetHashCode() : 0U;
        uint hashCode4 = value4 is not null ? (uint)value4.GetHashCode() : 0U;
        uint hashCode5 = value5 is not null ? (uint)value5.GetHashCode() : 0U;
        uint hashCode6 = value6 is not null ? (uint)value6.GetHashCode() : 0U;
        Initialize(out uint v1, out uint v2, out uint v3, out uint v4);
        v1 = Round(v1, hashCode1);
        v2 = Round(v2, hashCode2);
        v3 = Round(v3, hashCode3);
        v4 = Round(v4, hashCode4);
        return (int)MixFinal(QueueRound(QueueRound(MixState(v1, v2, v3, v4) + 24U, hashCode5), hashCode6));
    }

    public static int Combine<T1, T2, T3, T4, T5, T6, T7>(
        T1 value1,
        T2 value2,
        T3 value3,
        T4 value4,
        T5 value5,
        T6 value6,
        T7 value7
    )
    {
        uint hashCode1 = value1 is not null ? (uint)value1.GetHashCode() : 0U;
        uint hashCode2 = value2 is not null ? (uint)value2.GetHashCode() : 0U;
        uint hashCode3 = value3 is not null ? (uint)value3.GetHashCode() : 0U;
        uint hashCode4 = value4 is not null ? (uint)value4.GetHashCode() : 0U;
        uint hashCode5 = value5 is not null ? (uint)value5.GetHashCode() : 0U;
        uint hashCode6 = value6 is not null ? (uint)value6.GetHashCode() : 0U;
        uint hashCode7 = value7 is not null ? (uint)value7.GetHashCode() : 0U;
        Initialize(out uint v1, out uint v2, out uint v3, out uint v4);
        v1 = Round(v1, hashCode1);
        v2 = Round(v2, hashCode2);
        v3 = Round(v3, hashCode3);
        v4 = Round(v4, hashCode4);
        return (int)MixFinal(QueueRound(QueueRound(QueueRound(MixState(v1, v2, v3, v4) + 28U, hashCode5), hashCode6), hashCode7));
    }

    public static int Combine<T1, T2, T3, T4, T5, T6, T7, T8>(
        T1 value1,
        T2 value2,
        T3 value3,
        T4 value4,
        T5 value5,
        T6 value6,
        T7 value7,
        T8 value8
    )
    {
        uint hashCode1 = value1 is not null ? (uint)value1.GetHashCode() : 0U;
        uint hashCode2 = value2 is not null ? (uint)value2.GetHashCode() : 0U;
        uint hashCode3 = value3 is not null ? (uint)value3.GetHashCode() : 0U;
        uint hashCode4 = value4 is not null ? (uint)value4.GetHashCode() : 0U;
        uint hashCode5 = value5 is not null ? (uint)value5.GetHashCode() : 0U;
        uint hashCode6 = value6 is not null ? (uint)value6.GetHashCode() : 0U;
        uint hashCode7 = value7 is not null ? (uint)value7.GetHashCode() : 0U;
        uint hashCode8 = value8 is not null ? (uint)value8.GetHashCode() : 0U;
        Initialize(out uint v1, out uint v2, out uint v3, out uint v4);
        v1 = Round(v1, hashCode1);
        v2 = Round(v2, hashCode2);
        v3 = Round(v3, hashCode3);
        v4 = Round(v4, hashCode4);
        v1 = Round(v1, hashCode5);
        v2 = Round(v2, hashCode6);
        v3 = Round(v3, hashCode7);
        v4 = Round(v4, hashCode8);
        return (int)MixFinal(MixState(v1, v2, v3, v4) + 32U);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Initialize(out uint v1, out uint v2, out uint v3, out uint v4)
    {
        v1 = (uint)((int)seed - 1640531535 - 2048144777);
        v2 = seed + 2246822519U;
        v3 = seed;
        v4 = seed - 2654435761U;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Round(uint hash, uint input) => RotateLeft(hash + input * 2246822519U, 13) * 2654435761U;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint QueueRound(uint hash, uint queuedValue) => RotateLeft(hash + queuedValue * 3266489917U, 17) * 668265263U;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint MixState(uint v1, uint v2, uint v3, uint v4) => RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint RotateLeft(uint value, int offset) => value << offset | value >> 32 - offset;

    private static uint MixEmptyState() => seed + 374761393U;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint MixFinal(uint hash)
    {
        hash ^= hash >> 15;
        hash *= 2246822519U;
        hash ^= hash >> 13;
        hash *= 3266489917U;
        hash ^= hash >> 16;
        return hash;
    }

    public void Add<T>(T value) => Add(value is not null ? value.GetHashCode() : 0);

    public void Add<T>(T value, IEqualityComparer<T>? comparer) => Add(comparer?.GetHashCode(value) ?? (value is not null ? value.GetHashCode() : 0));

    private void Add(int value)
    {
        uint input = (uint)value;
        uint num = length++;
        switch (num % 4U)
        {
            case 0:
                queue1 = input;
                break;

            case 1:
                queue2 = input;
                break;

            case 2:
                queue3 = input;
                break;

            default:
                if (num == 3U)
                {
                    Initialize(out v1, out v2, out v3, out v4);
                }
                v1 = Round(v1, queue1);
                v2 = Round(v2, queue2);
                v3 = Round(v3, queue3);
                v4 = Round(v4, input);
                break;
        }
    }

    public int ToHashCode()
    {
        uint l = length;
        uint num = l % 4U;
        uint hash = (l < 4U ? MixEmptyState() : MixState(v1, v2, v3, v4)) + l * 4U;
        if (num > 0U)
        {
            hash = QueueRound(hash, queue1);
            if (num > 1U)
            {
                hash = QueueRound(hash, queue2);
                if (num > 2U)
                {
                    hash = QueueRound(hash, queue3);
                }
            }
        }
        return (int)MixFinal(hash);
    }

    [Obsolete(HashCodeMutable + " Use ToHashCode to retrieve the computed hash code.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode() => throw new NotSupportedException(HashCodeMutable);

    [Obsolete(HashCodeMutable, true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override bool Equals(object? obj) => throw new NotSupportedException(HashCodeMutable);
}
#endif
