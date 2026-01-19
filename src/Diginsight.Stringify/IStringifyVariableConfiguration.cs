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
}
