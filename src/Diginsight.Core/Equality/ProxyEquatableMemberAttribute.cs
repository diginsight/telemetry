namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ProxyEquatableMemberAttribute : EquatableMemberAttribute
{
    private object?[]? proxyArgs;

    protected override EqualityBehavior? Behavior => EqualityBehavior.Proxy;

    public Type ProxyType { get; }

    public string? ProxyMember { get; }

    public object?[] ProxyArgs
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

    public override IEquatableMemberDescriptor ToMemberDescriptor() =>
        new ProxyEquatableDescriptor(ProxyType, ProxyMember, ProxyArgs, OrderCore);
}
