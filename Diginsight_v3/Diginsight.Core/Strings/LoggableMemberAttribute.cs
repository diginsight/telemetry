namespace Diginsight.Strings;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class LoggableMemberAttribute : Attribute
{
    public string? Name { get; set; }
    public Type? Provider { get; set; }
}
