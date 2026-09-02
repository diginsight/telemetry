using System.Text.RegularExpressions;

namespace Diginsight.Stringify;

/// <summary>
/// Represents namespace matching configuration used while rendering type names.
/// </summary>
public interface IStringifyNamespaceConfiguration
{
    /// <summary>
    /// Gets the namespace pattern whose matches can be rendered implicitly.
    /// </summary>
    Regex? ImplicitNamespaces { get; }
    /// <summary>
    /// Gets the namespace pattern whose matches must be rendered explicitly.
    /// </summary>
    Regex? ExplicitNamespaces { get; }
    /// <summary>
    /// Gets a value indicating whether unspecified namespaces are rendered explicitly.
    /// </summary>
    bool IsNamespaceExplicitIfUnspecified { get; }
    /// <summary>
    /// Gets a value indicating whether ambiguous namespaces are rendered explicitly.
    /// </summary>
    bool IsNamespaceExplicitIfAmbiguous { get; }
}
