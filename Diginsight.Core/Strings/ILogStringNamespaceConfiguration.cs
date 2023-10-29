using System.Text.RegularExpressions;

namespace Diginsight.Strings;

public interface ILogStringNamespaceConfiguration
{
    Regex? ImplicitNamespaces { get; }
    Regex? ExplicitNamespaces { get; }
    bool IsNamespaceExplicitIfUnspecified { get; }
    bool IsNamespaceExplicitIfAmbiguous { get; }
}
