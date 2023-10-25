namespace Diginsight.Strings;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class LogStringableMemberAttribute : Attribute
{
    private object[]? providerArgs;

    public string? Name { get; set; }

    public Type? ProviderType { get; set; }

    public object[] ProviderArgs
    {
        get => providerArgs ??= Array.Empty<object>();
        set => providerArgs = value;
    }
}
