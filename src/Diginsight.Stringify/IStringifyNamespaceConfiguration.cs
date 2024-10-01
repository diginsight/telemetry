using System.Text.RegularExpressions;

namespace Diginsight.Stringify;

public interface IStringifyNamespaceConfiguration
{
    Regex? ImplicitNamespaces { get; }
    Regex? ExplicitNamespaces { get; }
    bool IsNamespaceExplicitIfUnspecified { get; }
    bool IsNamespaceExplicitIfAmbiguous { get; }
}
