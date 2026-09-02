using System.Text.RegularExpressions;

namespace Diginsight.Stringify;

/// <summary>
/// Represents mutable per-stringification configuration.
/// </summary>
public sealed class StringifyVariableConfiguration : IStringifyVariableConfiguration
{
    /// <summary>
    /// Gets the maximum string length.
    /// </summary>
    public Threshold MaxStringLength { get; set; }
    /// <summary>
    /// Gets the maximum number of collection items.
    /// </summary>
    public Threshold MaxCollectionItemCount { get; set; }
    /// <summary>
    /// Gets the maximum number of dictionary items.
    /// </summary>
    public InheritableThreshold MaxDictionaryItemCount { get; set; }
    /// <summary>
    /// Gets the maximum number of memberwise properties.
    /// </summary>
    public InheritableThreshold MaxMemberwisePropertyCount { get; set; }
    /// <summary>
    /// Gets the maximum number of anonymous object properties.
    /// </summary>
    public InheritableThreshold MaxAnonymousObjectPropertyCount { get; set; }
    /// <summary>
    /// Gets the maximum number of tuple items.
    /// </summary>
    public Threshold MaxTupleItemCount { get; set; }
    /// <summary>
    /// Gets the maximum number of method parameters.
    /// </summary>
    public Threshold MaxMethodParameterCount { get; set; }

    /// <summary>
    /// Gets the maximum nesting depth.
    /// </summary>
    public Threshold MaxDepth
    {
        get;
        set => field = value.Value == 0 ? throw new ArgumentOutOfRangeException(nameof(MaxDepth), "Expected positive value") : value;
    }

    /// <summary>
    /// Gets the namespace pattern whose matches can be rendered implicitly.
    /// </summary>
    public Regex? ImplicitNamespaces { get; set; }

    /// <summary>
    /// Gets the namespace pattern whose matches must be rendered explicitly.
    /// </summary>
    public Regex? ExplicitNamespaces { get; set; }

    /// <summary>
    /// Gets a value indicating whether unspecified namespaces are rendered explicitly.
    /// </summary>
    public bool IsNamespaceExplicitIfUnspecified { get; set; }

    /// <summary>
    /// Gets a value indicating whether ambiguous namespaces are rendered explicitly.
    /// </summary>
    public bool IsNamespaceExplicitIfAmbiguous { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringifyVariableConfiguration" /> class.
    /// </summary>
    /// <param name="source">The source configuration.</param>
    public StringifyVariableConfiguration(IStringifyVariableConfiguration source)
    {
        MaxStringLength = source.MaxStringLength;
        MaxCollectionItemCount = source.MaxCollectionItemCount;
        MaxDictionaryItemCount = source.MaxDictionaryItemCount;
        MaxMemberwisePropertyCount = source.MaxMemberwisePropertyCount;
        MaxAnonymousObjectPropertyCount = source.MaxAnonymousObjectPropertyCount;
        MaxTupleItemCount = source.MaxTupleItemCount;
        MaxMethodParameterCount = source.MaxMethodParameterCount;
        MaxDepth = source.MaxDepth;
        ImplicitNamespaces = source.ImplicitNamespaces;
        ExplicitNamespaces = source.ExplicitNamespaces;
        IsNamespaceExplicitIfUnspecified = source.IsNamespaceExplicitIfUnspecified;
        IsNamespaceExplicitIfAmbiguous = source.IsNamespaceExplicitIfAmbiguous;
    }
}
