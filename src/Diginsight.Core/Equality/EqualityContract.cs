using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Diginsight.Equality;

public abstract class EqualityContract : IEqualityContract
{
    private Type? comparerType;
    private object?[]? comparerArgs;
    private Type? proxyType;
    private object?[]? proxyArgs;

    private bool frozen = false;

    public EqualityBehavior? Behavior { get; private set; }

    protected Type ComparerType
    {
        get => comparerType ?? throw new InvalidOperationException($"{nameof(IComparerEquatableDescriptor.ComparerType)} is not set");
        private set => comparerType = value;
    }

    protected string? ComparerMember { get; private set; }

    [AllowNull]
    protected object?[] ComparerArgs
    {
        get => comparerArgs ??= [ ];
        private set => comparerArgs = value;
    }

    [AllowNull]
    protected Type ProxyType
    {
        get => proxyType ??= typeof(void);
        private set => proxyType = value;
    }

    protected string? ProxyMember { get; private set; }

    [AllowNull]
    protected object?[] ProxyArgs
    {
        get => proxyArgs ??= [ ];
        private set => proxyArgs = value;
    }

    internal void Freeze()
    {
        Behavior = EqualityBehavior.Default;
        frozen = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckFrozen()
    {
        if (frozen)
            throw new InvalidOperationException("Cannot change behavior of a frozen contract");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetBehavior(EqualityBehavior? behavior)
    {
        CheckFrozen();
        Behavior = behavior;
    }

    public void UnsetBehavior() => SetBehavior(null);

    public void SetAttributedBehavior() => SetBehavior(EqualityBehavior.Attributed);

    public void SetComparerBehavior(
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        Type comparerType,
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        object?[]? comparerArgs = null
    )
    {
        SetBehavior(EqualityBehavior.Comparer);
        ComparerType = comparerType;
        ComparerMember = null;
        ComparerArgs = comparerArgs;
    }

    public void SetComparerBehavior(
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        Type comparerType,
        string comparerMember,
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        object?[]? comparerArgs = null
    )
    {
        SetBehavior(EqualityBehavior.Comparer);
        ComparerType = comparerType;
        ComparerMember = comparerMember;
        ComparerArgs = comparerArgs;
    }

    public void SetDefaultBehavior() => SetBehavior(EqualityBehavior.Default);

    public void SetForbiddenBehavior() => SetBehavior(EqualityBehavior.Forbidden);

    public void SetIdentityBehavior() => SetBehavior(EqualityBehavior.Identity);

    public void SetProxyBehavior(
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        Type proxyType,
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        object?[]? proxyArgs = null
    )
    {
        SetBehavior(EqualityBehavior.Proxy);
        ProxyType = proxyType;
        ProxyMember = null;
        ProxyArgs = proxyArgs;
    }

    public void SetProxyBehavior(
        string proxyMember,
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        object?[]? proxyArgs = null
    )
    {
        SetBehavior(EqualityBehavior.Proxy);
        ProxyType = null;
        ProxyMember = proxyMember;
        ProxyArgs = proxyArgs;
    }

    public void SetProxyBehavior(
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        Type proxyType,
        string proxyMember,
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        object?[]? proxyArgs = null
    )
    {
        SetBehavior(EqualityBehavior.Proxy);
        ProxyType = proxyType;
        ProxyMember = proxyMember;
        ProxyArgs = proxyArgs;
    }
}
