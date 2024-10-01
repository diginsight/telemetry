#if !(NET || NETSTANDARD2_1_OR_GREATER)
using System.ComponentModel;

namespace Diginsight.Stringify;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class StringifyVariableConfigurationExtensions
{
    public static int? GetEffectiveMaxTotalLength(this IStringifyOverallConfiguration c) => c.MaxTotalLength.Value;

    public static int? GetEffectiveMaxStringLength(this IStringifyVariableConfiguration c) => c.MaxStringLength.Value;

    public static int? GetEffectiveMaxCollectionItemCount(this IStringifyVariableConfiguration c) => c.MaxCollectionItemCount.Value;

    public static int? GetEffectiveMaxDictionaryItemCount(this IStringifyVariableConfiguration c) => c.MaxDictionaryItemCount.GetValue(c.MaxCollectionItemCount);

    public static int? GetEffectiveMaxMemberwisePropertyCount(this IStringifyVariableConfiguration c) =>
        c.MaxMemberwisePropertyCount.GetValue(c.MaxCollectionItemCount, c.MaxDictionaryItemCount);

    public static int? GetEffectiveMaxAnonymousObjectPropertyCount(this IStringifyVariableConfiguration c) =>
        c.MaxAnonymousObjectPropertyCount.GetValue(c.MaxCollectionItemCount, c.MaxDictionaryItemCount, c.MaxMemberwisePropertyCount);

    public static int? GetEffectiveMaxTupleItemCount(this IStringifyVariableConfiguration c) => c.MaxTupleItemCount.Value;

    public static int? GetEffectiveMaxMethodParameterCount(this IStringifyVariableConfiguration c) => c.MaxMethodParameterCount.Value;

    public static int? GetEffectiveMaxDepth(this IStringifyVariableConfiguration c) => c.MaxDepth.Value;
}
#endif
