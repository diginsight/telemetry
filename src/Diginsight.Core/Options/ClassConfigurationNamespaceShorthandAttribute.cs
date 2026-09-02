namespace Diginsight.Options;

/// <summary>
/// Associates a shorthand alias with a namespace used in class-aware configuration keys.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ClassConfigurationNamespaceShorthandAttribute : Attribute
{
    /// <summary>
    /// Gets the namespace the shorthand refers to.
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// Gets the shorthand alias for the namespace.
    /// </summary>
    public string Shorthand { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassConfigurationNamespaceShorthandAttribute" /> class with the specified namespace and shorthand.
    /// </summary>
    /// <param name="namespace">The namespace the shorthand refers to.</param>
    /// <param name="shorthand">The shorthand alias for the namespace.</param>
    public ClassConfigurationNamespaceShorthandAttribute(string @namespace, string shorthand)
    {
        Namespace = @namespace;
        Shorthand = shorthand;
    }
}
