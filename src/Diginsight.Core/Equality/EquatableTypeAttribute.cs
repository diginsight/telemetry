namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
public sealed class EquatableTypeAttribute : Attribute, IEquatableTypeDescriptor
{
    private object[]? proxyArgs;

    public EqualityMode Mode { get; set; }

    public Type? ProxyType { get; set; }

    public object[] ProxyArgs
    {
        get => proxyArgs ??= [ ];
        set => proxyArgs = value;
    }
}
