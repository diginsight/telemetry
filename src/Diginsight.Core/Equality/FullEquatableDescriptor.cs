using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Equality;

internal sealed class FullEquatableDescriptor
    : IAttributedEquatableDescriptor
        , IDefaultEquatableDescriptor
        , IIdentityEquatableDescriptor
        , IProxyEquatableDescriptor
{
    private Type? proxyType;
    private object?[]? proxyArgs;

    [AllowNull]
    public Type ProxyType
    {
        get => proxyType ??= typeof(void);
        set => proxyType = value;
    }

    public string? ProxyMember { get; set; }

    [AllowNull]
    public object?[] ProxyArgs
    {
        get => proxyArgs ??= [ ];
        set => proxyArgs = value;
    }
}
