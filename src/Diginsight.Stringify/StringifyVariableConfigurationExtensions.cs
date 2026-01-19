using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Diginsight.Stringify;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class StringifyVariableConfigurationExtensions
{
    extension(IStringifyOverallConfiguration c)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("Use EffectiveMaxTotalLength instead")]
        public int? GetEffectiveMaxTotalLength() => c.EffectiveMaxTotalLength;

        public int? EffectiveMaxTotalLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => c.MaxTotalLength.Value;
        }
    }

    extension(IStringifyVariableConfiguration c)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("Use EffectiveMaxStringLength instead")]
        public int? GetEffectiveMaxStringLength() => c.EffectiveMaxStringLength;

        public int? EffectiveMaxStringLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => c.MaxStringLength.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("Use EffectiveMaxCollectionItemCount instead")]
        public int? GetEffectiveMaxCollectionItemCount() => c.EffectiveMaxCollectionItemCount;

        public int? EffectiveMaxCollectionItemCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => c.MaxCollectionItemCount.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("Use EffectiveMaxDictionaryItemCount instead")]
        public int? GetEffectiveMaxDictionaryItemCount() => c.EffectiveMaxDictionaryItemCount;

        public int? EffectiveMaxDictionaryItemCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => c.MaxDictionaryItemCount.GetValue(c.MaxCollectionItemCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("Use EffectiveMaxMemberwisePropertyCount instead")]
        public int? GetEffectiveMaxMemberwisePropertyCount() => c.EffectiveMaxMemberwisePropertyCount;

        public int? EffectiveMaxMemberwisePropertyCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => c.MaxMemberwisePropertyCount.GetValue(c.MaxCollectionItemCount, c.MaxDictionaryItemCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("Use EffectiveMaxAnonymousObjectPropertyCount instead")]
        public int? GetEffectiveMaxAnonymousObjectPropertyCount() => c.EffectiveMaxAnonymousObjectPropertyCount;

        public int? EffectiveMaxAnonymousObjectPropertyCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => c.MaxAnonymousObjectPropertyCount.GetValue(c.MaxCollectionItemCount, c.MaxDictionaryItemCount, c.MaxMemberwisePropertyCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("Use EffectiveMaxTupleItemCount instead")]
        public int? GetEffectiveMaxTupleItemCount() => c.EffectiveMaxTupleItemCount;

        public int? EffectiveMaxTupleItemCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => c.MaxTupleItemCount.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("Use EffectiveMaxMethodParameterCount instead")]
        public int? GetEffectiveMaxMethodParameterCount() => c.EffectiveMaxMethodParameterCount;

        public int? EffectiveMaxMethodParameterCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => c.MaxMethodParameterCount.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("Use EffectiveMaxDepth instead")]
        public int? GetEffectiveMaxDepth() => c.EffectiveMaxDepth;

        public int? EffectiveMaxDepth
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => c.MaxDepth.Value;
        }
    }
}
