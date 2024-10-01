using System.Text.RegularExpressions;

namespace Diginsight.Stringify;

public sealed class StringifyVariableConfiguration : IStringifyVariableConfiguration
{
    private Threshold maxDepth;

    public Threshold MaxStringLength { get; set; }
    public Threshold MaxCollectionItemCount { get; set; }
    public InheritableThreshold MaxDictionaryItemCount { get; set; }
    public InheritableThreshold MaxMemberwisePropertyCount { get; set; }
    public InheritableThreshold MaxAnonymousObjectPropertyCount { get; set; }
    public Threshold MaxTupleItemCount { get; set; }
    public Threshold MaxMethodParameterCount { get; set; }

    public Threshold MaxDepth
    {
        get => maxDepth;
        set => maxDepth = value.Value == 0 ? throw new ArgumentOutOfRangeException(nameof(MaxDepth), "Expected positive value") : value;
    }

    public Regex? ImplicitNamespaces { get; set; }

    public Regex? ExplicitNamespaces { get; set; }

    public bool IsNamespaceExplicitIfUnspecified { get; set; }

    public bool IsNamespaceExplicitIfAmbiguous { get; set; }

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
