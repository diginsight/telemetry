using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Equality;

internal sealed class FullEquatableDescriptor
    : IAttributedEquatableDescriptor
        , IDefaultEquatableDescriptor
        , IIdentityEquatableDescriptor
        , IProxyEquatableDescriptor
        , IComparerEquatableDescriptor
{
    private Type? proxyType;
    private object?[]? proxyArgs;
    private Type? comparerType;
    private object?[]? comparerArgs;

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

    [AllowNull]
    public Type ComparerType
    {
        get => comparerType ??= typeof(void);
        set => comparerType = value;
    }

    public string? ComparerMember { get; set; }

    [AllowNull]
    public object?[] ComparerArgs
    {
        get => comparerArgs ??= [ ];
        set => comparerArgs = value;
    }
}
