namespace Diginsight.Stringify;

/// <summary>
/// Represents per-stringification variable configuration.
/// </summary>
public interface IStringifyVariableConfiguration : IStringifyNamespaceConfiguration
{
    /// <summary>
    /// Gets the maximum string length.
    /// </summary>
    Threshold MaxStringLength { get; }
    /// <summary>
    /// Gets the maximum number of collection items.
    /// </summary>
    Threshold MaxCollectionItemCount { get; }
    /// <summary>
    /// Gets the maximum number of dictionary items.
    /// </summary>
    InheritableThreshold MaxDictionaryItemCount { get; }
    /// <summary>
    /// Gets the maximum number of memberwise properties.
    /// </summary>
    InheritableThreshold MaxMemberwisePropertyCount { get; }
    /// <summary>
    /// Gets the maximum number of anonymous object properties.
    /// </summary>
    InheritableThreshold MaxAnonymousObjectPropertyCount { get; }
    /// <summary>
    /// Gets the maximum number of tuple items.
    /// </summary>
    Threshold MaxTupleItemCount { get; }
    /// <summary>
    /// Gets the maximum number of method parameters.
    /// </summary>
    Threshold MaxMethodParameterCount { get; }
    /// <summary>
    /// Gets the maximum nesting depth.
    /// </summary>
    Threshold MaxDepth { get; }
}
