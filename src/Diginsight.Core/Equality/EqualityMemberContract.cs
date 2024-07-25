namespace Diginsight.Equality;

public class EqualityMemberContract : EqualityContract, IEqualityMemberContract
{
    private readonly Type? memberType;

    public int? Order { get; set; }

    public bool? Included { get; set; }

    protected override bool Excluded => Included == false;

    private protected EqualityMemberContract(Type memberType)
    {
        this.memberType = memberType;
    }

    internal static EqualityMemberContract For(Type memberType)
    {
        return (EqualityMemberContract)Activator.CreateInstance(typeof(EqualityMemberContract<>).MakeGenericType(memberType))!;
    }
}

public sealed class EqualityMemberContract<T> : EqualityMemberContract
{
    public EqualityMemberContract()
        : base(typeof(T)) { }
}
