using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Diginsight.Stringify;

/// <summary>
/// Provides extension methods for resolving effective stringify configuration values.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class StringifyVariableConfigurationExtensions
{
    /// <param name="c">The IStringifyOverallConfiguration instance.</param>
    extension(IStringifyOverallConfiguration c)
    {
        /// <summary>
        /// Gets the effective max total length.
        /// </summary>
        /// <returns>The effective maximum total output length.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("Use EffectiveMaxTotalLength instead")]
        public int? GetEffectiveMaxTotalLength() => c.EffectiveMaxTotalLength;

        /// <summary>
        /// Gets the effective maximum total output length.
        /// </summary>
        public int? EffectiveMaxTotalLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => c.MaxTotalLength.Value;
        }
    }

    /// <param name="c">The IStringifyVariableConfiguration instance.</param>
    extension(IStringifyVariableConfiguration c)
    {
        /// <summary>
        /// Gets the effective max string length.
        /// </summary>
        /// <returns>The effective maximum string length.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("Use EffectiveMaxStringLength instead")]
        public int? GetEffectiveMaxStringLength() => c.EffectiveMaxStringLength;

        /// <summary>
        /// Gets the effective maximum string length.
        /// </summary>
        public int? EffectiveMaxStringLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => c.MaxStringLength.Value;
        }

        /// <summary>
        /// Gets the effective max collection item count.
        /// </summary>
        /// <returns>The effective maximum collection item count.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("Use EffectiveMaxCollectionItemCount instead")]
        public int? GetEffectiveMaxCollectionItemCount() => c.EffectiveMaxCollectionItemCount;

        /// <summary>
        /// Gets the effective maximum collection item count.
        /// </summary>
        public int? EffectiveMaxCollectionItemCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => c.MaxCollectionItemCount.Value;
        }

        /// <summary>
        /// Gets the effective max dictionary item count.
        /// </summary>
        /// <returns>The effective maximum dictionary item count.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("Use EffectiveMaxDictionaryItemCount instead")]
        public int? GetEffectiveMaxDictionaryItemCount() => c.EffectiveMaxDictionaryItemCount;

        /// <summary>
        /// Gets the effective maximum dictionary item count.
        /// </summary>
        public int? EffectiveMaxDictionaryItemCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => c.MaxDictionaryItemCount.GetValue(c.MaxCollectionItemCount);
        }

        /// <summary>
        /// Gets the effective max memberwise property count.
        /// </summary>
        /// <returns>The effective maximum memberwise property count.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("Use EffectiveMaxMemberwisePropertyCount instead")]
        public int? GetEffectiveMaxMemberwisePropertyCount() => c.EffectiveMaxMemberwisePropertyCount;

        /// <summary>
        /// Gets the effective maximum memberwise property count.
        /// </summary>
        public int? EffectiveMaxMemberwisePropertyCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => c.MaxMemberwisePropertyCount.GetValue(c.MaxCollectionItemCount, c.MaxDictionaryItemCount);
        }

        /// <summary>
        /// Gets the effective max anonymous object property count.
        /// </summary>
        /// <returns>The effective maximum anonymous object property count.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("Use EffectiveMaxAnonymousObjectPropertyCount instead")]
        public int? GetEffectiveMaxAnonymousObjectPropertyCount() => c.EffectiveMaxAnonymousObjectPropertyCount;

        /// <summary>
        /// Gets the effective maximum anonymous object property count.
        /// </summary>
        public int? EffectiveMaxAnonymousObjectPropertyCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => c.MaxAnonymousObjectPropertyCount.GetValue(c.MaxCollectionItemCount, c.MaxDictionaryItemCount, c.MaxMemberwisePropertyCount);
        }

        /// <summary>
        /// Gets the effective max tuple item count.
        /// </summary>
        /// <returns>The effective maximum tuple item count.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("Use EffectiveMaxTupleItemCount instead")]
        public int? GetEffectiveMaxTupleItemCount() => c.EffectiveMaxTupleItemCount;

        /// <summary>
        /// Gets the effective maximum tuple item count.
        /// </summary>
        public int? EffectiveMaxTupleItemCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => c.MaxTupleItemCount.Value;
        }

        /// <summary>
        /// Gets the effective max method parameter count.
        /// </summary>
        /// <returns>The effective maximum method parameter count.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("Use EffectiveMaxMethodParameterCount instead")]
        public int? GetEffectiveMaxMethodParameterCount() => c.EffectiveMaxMethodParameterCount;

        /// <summary>
        /// Gets the effective maximum method parameter count.
        /// </summary>
        public int? EffectiveMaxMethodParameterCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => c.MaxMethodParameterCount.Value;
        }

        /// <summary>
        /// Gets the effective max depth.
        /// </summary>
        /// <returns>The effective maximum depth.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete("Use EffectiveMaxDepth instead")]
        public int? GetEffectiveMaxDepth() => c.EffectiveMaxDepth;

        /// <summary>
        /// Gets the effective maximum depth.
        /// </summary>
        public int? EffectiveMaxDepth
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => c.MaxDepth.Value;
        }
    }
}
