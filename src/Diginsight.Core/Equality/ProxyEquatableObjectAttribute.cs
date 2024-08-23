namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
public sealed class ProxyEquatableObjectAttribute
    : EquatableObjectAttribute, IProxyEquatableObjectDescriptor, IProxyEquatableMemberDescriptor
{
    private object?[]? proxyArgs;

    public override EqualityBehavior Behavior => EqualityBehavior.Proxy;

    public Type ProxyType { get; }

    public string? ProxyMember { get; }

    public object?[] ProxyArgs
    {
        get => proxyArgs ??= [ ];
        set => proxyArgs = value;
    }

    public ProxyEquatableObjectAttribute(Type proxyType)
    {
        ProxyType = proxyType;
    }

    public ProxyEquatableObjectAttribute(string proxyMember)
        : this(typeof(void), proxyMember) { }

    public ProxyEquatableObjectAttribute(Type proxyType, string proxyMember)
    {
        ProxyType = proxyType;
        ProxyMember = proxyMember;
    }
}
