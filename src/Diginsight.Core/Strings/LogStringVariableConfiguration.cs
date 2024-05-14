using System.Text.RegularExpressions;

namespace Diginsight.Strings;

public sealed class LogStringVariableConfiguration : ILogStringVariableConfiguration
{
    private LogThreshold maxDepth;

    public LogThreshold MaxStringLength { get; set; }
    public LogThreshold MaxCollectionItemCount { get; set; }
    public InheritableLogThreshold MaxDictionaryItemCount { get; set; }
    public InheritableLogThreshold MaxMemberwisePropertyCount { get; set; }
    public InheritableLogThreshold MaxAnonymousObjectPropertyCount { get; set; }
    public LogThreshold MaxTupleItemCount { get; set; }
    public LogThreshold MaxMethodParameterCount { get; set; }

    public LogThreshold MaxDepth
    {
        get => maxDepth;
        set => maxDepth = value.Value == 0 ? throw new ArgumentOutOfRangeException(nameof(MaxDepth), "Expected positive value") : value;
    }

    public Regex? ImplicitNamespaces { get; set; }

    public Regex? ExplicitNamespaces { get; set; }

    public bool IsNamespaceExplicitIfUnspecified { get; set; }

    public bool IsNamespaceExplicitIfAmbiguous { get; set; }

    public LogStringVariableConfiguration(ILogStringVariableConfiguration source)
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
