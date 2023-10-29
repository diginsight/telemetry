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
}
