#if !(NET || NETSTANDARD2_1_OR_GREATER)
using System.ComponentModel;

namespace Diginsight.Strings;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class LogStringVariableConfigurationExtensions
{
    public static int? GetEffectiveMaxTotalLength(this ILogStringOverallConfiguration c) => c.MaxTotalLength.Value;

    public static int? GetEffectiveMaxStringLength(this ILogStringVariableConfiguration c) => c.MaxStringLength.Value;

    public static int? GetEffectiveMaxCollectionItemCount(this ILogStringVariableConfiguration c) => c.MaxCollectionItemCount.Value;

    public static int? GetEffectiveMaxDictionaryItemCount(this ILogStringVariableConfiguration c) => c.MaxDictionaryItemCount.GetValue(c.MaxCollectionItemCount);

    public static int? GetEffectiveMaxMemberwisePropertyCount(this ILogStringVariableConfiguration c) =>
        c.MaxMemberwisePropertyCount.GetValue(c.MaxCollectionItemCount, c.MaxDictionaryItemCount);

    public static int? GetEffectiveMaxAnonymousObjectPropertyCount(this ILogStringVariableConfiguration c) =>
        c.MaxAnonymousObjectPropertyCount.GetValue(c.MaxCollectionItemCount, c.MaxDictionaryItemCount, c.MaxMemberwisePropertyCount);

    public static int? GetEffectiveMaxTupleItemCount(this ILogStringVariableConfiguration c) => c.MaxTupleItemCount.Value;

    public static int? GetEffectiveMaxMethodParameterCount(this ILogStringVariableConfiguration c) => c.MaxMethodParameterCount.Value;

    public static int? GetEffectiveMaxDepth(this ILogStringVariableConfiguration c) => c.MaxDepth.Value;
}
#endif
