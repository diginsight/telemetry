namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ProxyEquatableMemberAttribute : EquatableMemberAttribute, IProxyEquatableMemberDescriptor
{
    private object[]? proxyArgs;

    public Type ProxyType { get; }

    public string? ProxyMember { get; }

    public object[] ProxyArgs
    {
        get => proxyArgs ??= [ ];
        set => proxyArgs = value;
    }

    public ProxyEquatableMemberAttribute(Type proxyType)
    {
        ProxyType = proxyType;
    }

    public ProxyEquatableMemberAttribute(string proxyMember)
        : this(typeof(void), proxyMember) { }

    public ProxyEquatableMemberAttribute(Type proxyType, string proxyMember)
    {
        ProxyType = proxyType;
        ProxyMember = proxyMember;
    }
}
