namespace Diginsight.SmartCache.Externalization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Assembly, Inherited = false)]
public sealed class CacheInterchangeNameAttribute : Attribute
{
    public string Name { get; }

    public CacheInterchangeNameAttribute(string name)
    {
        Name = name;
    }
}
