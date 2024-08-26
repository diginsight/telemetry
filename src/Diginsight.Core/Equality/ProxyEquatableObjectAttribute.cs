namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
public sealed class ProxyEquatableObjectAttribute : EquatableObjectAttribute
{
    private object?[]? proxyArgs;

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

    public override IEquatableObjectDescriptor ToObjectDescriptor() =>
        new ProxyEquatableDescriptor(ProxyType, ProxyMember, ProxyArgs, null);
}
