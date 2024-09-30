namespace Diginsight.Stringify;

public interface IStringifyVariableConfiguration : IStringifyNamespaceConfiguration
{
    Threshold MaxStringLength { get; }
    Threshold MaxCollectionItemCount { get; }
    InheritableThreshold MaxDictionaryItemCount { get; }
    InheritableThreshold MaxMemberwisePropertyCount { get; }
    InheritableThreshold MaxAnonymousObjectPropertyCount { get; }
    Threshold MaxTupleItemCount { get; }
    Threshold MaxMethodParameterCount { get; }
    Threshold MaxDepth { get; }

#if NET || NETSTANDARD2_1_OR_GREATER
    sealed int? GetEffectiveMaxStringLength() => MaxStringLength.Value;

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
