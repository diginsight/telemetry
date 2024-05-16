namespace Diginsight.Equality;

public class EqualityMemberContract : IEqualityMemberContract
{
    public static readonly IEqualityMemberContract Empty = new EqualityMemberContract();

    private readonly Type? memberType;

    protected object[]? proxyArgs;
    protected object[]? comparerArgs;

    public bool? Included { get; set; }

    public bool ByReference { get; set; }

    public Type? ProxyType { get; set; }

    public object[] ProxyArgs
    {
        get => proxyArgs ??= [ ];
        set => proxyArgs = value;
    }

    public Type? ComparerType { get; set; }

    public string? ComparerMember { get; set; }

    public object[] ComparerArgs
    {
        get => comparerArgs ??= [ ];
        set => comparerArgs = value;
    }

    public int? Order { get; set; }

    private EqualityMemberContract()
    {
        memberType = null;
    }

    private protected EqualityMemberContract(Type memberType)
    {
        this.memberType = memberType;
    }

    internal static EqualityMemberContract For(Type memberType)
    {
        return (EqualityMemberContract)Activator.CreateInstance(typeof(EqualityMemberContract<>).MakeGenericType(memberType))!;
    }

    public EqualityMemberContract WithCustomTypeContract(Action<EqualityTypeContract> configureContract)
    {
        throw new NotImplementedException();
        //EquatableTypeContract typeContract = LogStringTypeContract.For(memberType ?? throw new UnreachableException("Dummy member contract"));
        //configureContract(typeContract);

        //proxyType = typeof(CustomMemberwiseLogStringProvider);
        //proxyArgs = [ typeContract ];

        //return this;
    }

    //public Type? ProxyType
    //{
    //    get => proxyCtor?.DeclaringType;
    //    set
    //    {
    //        if (memberType is null)
    //        {
    //            throw new InvalidOperationException("Dummy member contract");
    //        }

    //        if (value is null)
    //        {
    //            proxyCtor = null;
    //            return;
    //        }

    //        (ConstructorInfo Ctor, Type FirstParamType)[] candidateCtorPairs = value.GetConstructors()
    //            .Select(static c => (Ctor: c, FirstParamType: c.GetParameters().FirstOrDefault()?.ParameterType))
    //            .Where(static x => x.FirstParamType is not null)
    //            .Select(static x => (x.Ctor, x.FirstParamType!))
    //            .ToArray();

    //        if (!(candidateCtorPairs.Length > 0))
    //        {
    //            throw new ArgumentException($"Type '{value.Name}' has no suitable constructor");
    //        }

    //        (ConstructorInfo bestCtor, Type bestFirstParamType) = candidateCtorPairs[0];
    //        foreach ((ConstructorInfo currentCtor, Type currentFirstParamType) in candidateCtorPairs.Skip(1))
    //        {
    //            if (bestFirstParamType == currentFirstParamType)
    //            {
    //                throw new ArgumentException($"Type '{value.Name}' has no best constructor");
    //            }
    //            if (bestFirstParamType == memberType)
    //            {
    //                break;
    //            }
    //            if (bestFirstParamType.IsAssignableFrom(currentFirstParamType))
    //            {
    //                (bestCtor, bestFirstParamType) = (currentCtor, currentFirstParamType);
    //            }
    //        }

    //        proxyCtor = bestCtor;
    //    }
    //}
}

public sealed class EqualityMemberContract<T> : EqualityMemberContract
{
    public EqualityMemberContract()
        : base(typeof(T)) { }

    public EqualityMemberContract<T> WithCustomTypeContract(Action<EqualityTypeContract<T>> configureContract)
    {
        throw new NotImplementedException();
        //EquatableTypeContract<T> typeContract = new ();
        //configureContract(typeContract);

        //providerType = typeof(CustomMemberwiseLogStringProvider);
        //providerArgs = [ typeContract ];

        //return this;
    }
}
