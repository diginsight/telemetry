using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Diginsight.Equality;

public abstract class EqualityContract : IEqualityContract, IComparerEquatableDescriptor, IProxyEquatableDescriptor
{
    private Type? comparerType;
    private string? comparerMember;
    private object?[]? comparerArgs;
    private Type? proxyType;
    private string? proxyMember;
    private object?[]? proxyArgs;

    private bool frozen = false;

    public EqualityBehavior? Behavior { get; private set; }

    EqualityBehavior IEquatableDescriptor.Behavior => Behavior ?? default;

    IComparerEquatableDescriptor? IEqualityContract.ComparerDescriptor => Behavior == EqualityBehavior.Comparer ? this : null;

    IProxyEquatableDescriptor? IEqualityContract.ProxyDescriptor => Behavior == EqualityBehavior.Proxy ? this : null;

    Type IProxyEquatableDescriptor.ProxyType => proxyType ??= typeof(void);

    string? IProxyEquatableDescriptor.ProxyMember => proxyMember;

    object?[] IProxyEquatableDescriptor.ProxyArgs => proxyArgs ??= [ ];

    Type IComparerEquatableDescriptor.ComparerType => comparerType ??= typeof(void);

    string? IComparerEquatableDescriptor.ComparerMember => comparerMember;

    object?[] IComparerEquatableDescriptor.ComparerArgs => comparerArgs ??= [ ];

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
        this.comparerType = comparerType;
        comparerMember = null;
        this.comparerArgs = comparerArgs;
    }

    public void SetComparerBehavior(
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        string comparerMember,
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        object?[]? comparerArgs = null
    )
    {
        SetBehavior(EqualityBehavior.Comparer);
        comparerType = null;
        this.comparerMember = comparerMember;
        this.comparerArgs = comparerArgs;
    }

    public void SetComparerBehavior(
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        Type comparerType,
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        string comparerMember,
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        object?[]? comparerArgs = null
    )
    {
        SetBehavior(EqualityBehavior.Comparer);
        this.comparerType = comparerType;
        this.comparerMember = comparerMember;
        this.comparerArgs = comparerArgs;
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
        this.proxyType = proxyType;
        proxyMember = null;
        this.proxyArgs = proxyArgs;
    }

    public void SetProxyBehavior(
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        string proxyMember,
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        object?[]? proxyArgs = null
    )
    {
        SetBehavior(EqualityBehavior.Proxy);
        proxyType = null;
        this.proxyMember = proxyMember;
        this.proxyArgs = proxyArgs;
    }

    public void SetProxyBehavior(
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        Type proxyType,
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        string proxyMember,
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        object?[]? proxyArgs = null
    )
    {
        SetBehavior(EqualityBehavior.Proxy);
        this.proxyType = proxyType;
        this.proxyMember = proxyMember;
        this.proxyArgs = proxyArgs;
    }
}
