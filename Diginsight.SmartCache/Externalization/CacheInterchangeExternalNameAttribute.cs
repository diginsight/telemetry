namespace Diginsight.SmartCache.Externalization;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class CacheInterchangeExternalNameAttribute : Attribute
{
    public string Name { get; }
    public Type? TargetType { get; }
    public string? TargetAssembly { get; }

    public CacheInterchangeExternalNameAttribute(string name, Type targetType)
    {
        Name = name;
        TargetType = targetType;
    }

    public CacheInterchangeExternalNameAttribute(string name, string targetAssembly)
    {
        Name = name;
        TargetAssembly = targetAssembly;
    }
}
