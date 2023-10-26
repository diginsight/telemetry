namespace Diginsight.Strings;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class LogStringableMemberAttribute : Attribute, ILogStringableMemberDescriptor
{
    private object[]? providerArgs;
    private int order;
    private bool isOrderSet;

    public string? Name { get; set; }

    public Type? ProviderType { get; set; }

    public object[] ProviderArgs
    {
        get => providerArgs ??= Array.Empty<object>();
        set => providerArgs = value;
    }

    public int Order
    {
        get => order;
        set
        {
            isOrderSet = true;
            order = value;
        }
    }

    int? ILogStringableMemberDescriptor.Order => isOrderSet ? order : null;
}
