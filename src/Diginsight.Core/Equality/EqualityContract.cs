using System.Runtime.CompilerServices;

namespace Diginsight.Equality;

public abstract class EqualityContract : IEqualityContract
{
    private readonly FullEquatableDescriptor fullDescriptor = new ();

    private bool frozen = false;

    IAttributedEquatableDescriptor? IEqualityContract.AttributedDescriptor =>
        !Excluded && Behavior == EqualityBehavior.Attributed ? fullDescriptor : null;

    IDefaultEquatableDescriptor? IEqualityContract.DefaultDescriptor =>
        !Excluded && Behavior == EqualityBehavior.Default ? fullDescriptor : null;

    IIdentityEquatableDescriptor? IEqualityContract.IdentityDescriptor =>
        !Excluded && Behavior == EqualityBehavior.Identity ? fullDescriptor : null;

    IProxyEquatableDescriptor? IEqualityContract.ProxyDescriptor =>
        !Excluded && Behavior == EqualityBehavior.Proxy ? fullDescriptor : null;

    IComparerEquatableDescriptor? IEqualityContract.ComparerDescriptor =>
        !Excluded && Behavior == EqualityBehavior.Comparer ? fullDescriptor : null;

    public EqualityBehavior? Behavior { get; private set; }

    protected virtual bool Excluded => false;

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

    public void SetDefaultBehavior() => SetBehavior(EqualityBehavior.Default);

    public void SetAttributedBehavior() => SetBehavior(EqualityBehavior.Attributed);

    public void SetIdentityBehavior() => SetBehavior(EqualityBehavior.Identity);

    public void SetProxyBehavior(Type proxyType, object?[]? proxyArgs = null)
    {
        SetBehavior(EqualityBehavior.Proxy);
        fullDescriptor.ProxyType = proxyType;
        fullDescriptor.ProxyMember = null;
        fullDescriptor.ProxyArgs = proxyArgs;
    }

    public void SetProxyBehavior(string proxyMember, object?[]? proxyArgs = null)
    {
        SetBehavior(EqualityBehavior.Proxy);
        fullDescriptor.ProxyType = null;
        fullDescriptor.ProxyMember = proxyMember;
        fullDescriptor.ProxyArgs = proxyArgs;
    }

    public void SetProxyBehavior(Type proxyType, string proxyMember, object?[]? proxyArgs = null)
    {
        SetBehavior(EqualityBehavior.Proxy);
        fullDescriptor.ProxyType = proxyType;
        fullDescriptor.ProxyMember = proxyMember;
        fullDescriptor.ProxyArgs = proxyArgs;
    }

    public void SetComparerBehavior(Type comparerType, object?[]? comparerArgs = null)
    {
        SetBehavior(EqualityBehavior.Comparer);
        fullDescriptor.ComparerType = comparerType;
        fullDescriptor.ComparerMember = null;
        fullDescriptor.ComparerArgs = comparerArgs;
    }

    public void SetComparerBehavior(string comparerMember, object?[]? comparerArgs = null)
    {
        SetBehavior(EqualityBehavior.Comparer);
        fullDescriptor.ComparerType = null;
        fullDescriptor.ComparerMember = comparerMember;
        fullDescriptor.ComparerArgs = comparerArgs;
    }

    public void SetComparerBehavior(Type comparerType, string comparerMember, object?[]? comparerArgs = null)
    {
        SetBehavior(EqualityBehavior.Comparer);
        fullDescriptor.ComparerType = comparerType;
        fullDescriptor.ComparerMember = comparerMember;
        fullDescriptor.ComparerArgs = comparerArgs;
    }
}
