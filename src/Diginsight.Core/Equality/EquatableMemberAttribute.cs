namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class EquatableMemberAttribute : Attribute, IEquatableMemberDescriptor
{
    private object[]? proxyArgs;
    private object[]? comparerArgs;
    private int order;
    private bool isOrderSet;

    public bool ByReference { get; set; }

    public Type? ProxyType { get; set; }

    public object[] ProxyArgs
    {
        get => proxyArgs ??= [ ];
        set => proxyArgs = value;
    }

    public Type? ComparerType { get; set; }

    public string? ComparerMember { get; set; }

    public object[] ComparerArgs
    {
        get => comparerArgs ??= [ ];
        set => comparerArgs = value;
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

    int? IEquatableMemberDescriptor.Order => isOrderSet ? order : null;
}
