using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Diginsight.Equality;

public abstract class EqualityContract : IEqualityContract
{
    private static readonly int AllAllowedBehaviors =
#if NET
        Enum.GetValues<EqualityBehavior>()
#else
        Enum.GetValues(typeof(EqualityBehavior)).Cast<EqualityBehavior>()
#endif
            .Sum(static x => 1 << (int)x);

    private static readonly ISet<Type> BasicTypes = new HashSet<Type>()
    {
        typeof(string),
        typeof(decimal),
        typeof(StringBuilder),
        typeof(Regex),
        typeof(Uri),
        typeof(Index),
        typeof(Range),
        typeof(DateTime),
        typeof(DateTimeOffset),
#if NET
        typeof(DateOnly),
        typeof(TimeOnly),
#endif
        typeof(TimeSpan),
        typeof(Guid),
    };

    private readonly int allowedBehaviors;

    private Type? comparerType;
    private object?[]? comparerArgs;
    private Type? proxyType;
    private object?[]? proxyArgs;

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

    protected EqualityContract(Type type)
    {
        allowedBehaviors = GetBehaviorBaseline(type).Allowed;
    }

    private static (EqualityBehavior Fallback, int Allowed) GetBehaviorBaseline(Type type)
    {
        if (type.IsPointer)
        {
            throw new ArgumentException("Pointer types are not supported", nameof(type));
        }
#if NET || NETSTANDARD2_1_OR_GREATER
        if (type.IsByRefLike)
        {
            throw new ArgumentException("Byref-like types are not supported", nameof(type));
        }
#endif

        while (type.IsGenericParameter)
        {
            type = type.GetGenericParameterConstraints().FirstOrDefault() ?? typeof(object);
        }

        const int identityAllowedBehaviors = (1 << (int)EqualityBehavior.Forbidden) + (1 << (int)EqualityBehavior.Identity);
        const int basicAllowedBehaviors = identityAllowedBehaviors + (1 << (int)EqualityBehavior.Default) + (1 << (int)EqualityBehavior.Comparer);

        if (type.IsBanned())
        {
            return (EqualityBehavior.Forbidden, 1 << (int)EqualityBehavior.Forbidden);
        }

        if (type.IsEnum || type.IsPrimitive || BasicTypes.Contains(type))
        {
            return (EqualityBehavior.Default, basicAllowedBehaviors);
        }

        if (typeof(Delegate).IsAssignableFrom(type))
        {
            return (EqualityBehavior.Identity, identityAllowedBehaviors);
        }

        return (EqualityBehavior.Structural, AllAllowedBehaviors);
    }

    protected static EqualityBehavior GetFallbackBehavior(Type type) => GetBehaviorBaseline(type).Fallback;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetBehavior(EqualityBehavior? behavior)
    {
        if (behavior is { } behavior0 && (allowedBehaviors & (1 << (int)behavior0)) == 0)
        {
            throw new ArgumentException($"{nameof(EqualityBehavior)}.{behavior0:G} is not allowed for this contract");
        }

        Behavior = behavior;
    }

    public void UnsetBehavior() => SetBehavior(null);

    public void SetStructuralBehavior() => SetBehavior(EqualityBehavior.Structural);

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
