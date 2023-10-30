namespace Diginsight.Strings;

public interface ILogStringVariableConfiguration : ILogStringNamespaceConfiguration
{
    LogThreshold MaxCollectionItemCount { get; }
    InheritableLogThreshold MaxDictionaryItemCount { get; }
    InheritableLogThreshold MaxMemberwisePropertyCount { get; }
    InheritableLogThreshold MaxAnonymousObjectPropertyCount { get; }
    LogThreshold MaxTupleItemCount { get; }
    LogThreshold MaxMethodParameterCount { get; }
    LogThreshold MaxDepth { get; }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    sealed int? GetEffectiveMaxCollectionItemCount() => MaxCollectionItemCount.Value;

    sealed int? GetEffectiveMaxDictionaryItemCount() => MaxDictionaryItemCount.GetValue(MaxCollectionItemCount);

    sealed int? GetEffectiveMaxMemberwisePropertyCount() =>
        MaxMemberwisePropertyCount.GetValue(MaxCollectionItemCount, MaxDictionaryItemCount);

    sealed int? GetEffectiveMaxAnonymousObjectPropertyCount() =>
        MaxAnonymousObjectPropertyCount.GetValue(MaxCollectionItemCount, MaxDictionaryItemCount, MaxMemberwisePropertyCount);

    sealed int? GetEffectiveMaxTupleItemCount() => MaxTupleItemCount.Value;

    sealed int? GetEffectiveMaxMethodParameterCount() => MaxMethodParameterCount.Value;

    sealed int? GetEffectiveMaxDepth() => MaxDepth.Value;
#endif
}
