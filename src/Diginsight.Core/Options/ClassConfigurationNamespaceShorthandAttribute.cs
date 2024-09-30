namespace Diginsight.Options;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ClassConfigurationNamespaceShorthandAttribute : Attribute
{
    public string Namespace { get; }
    public string Shorthand { get; }

    public ClassConfigurationNamespaceShorthandAttribute(string @namespace, string shorthand)
    {
        Namespace = @namespace;
        Shorthand = shorthand;
    }
}
