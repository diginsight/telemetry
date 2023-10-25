namespace Diginsight.Strings;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class LogStringableMemberAttribute : Attribute
{
    public string? Name { get; set; }

    public Type? ProviderType { get; set; }

    public object?[]? ProviderArgs { get; set; }
}
