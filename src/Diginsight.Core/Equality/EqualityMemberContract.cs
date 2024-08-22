using System.Diagnostics;
using System.Reflection;

namespace Diginsight.Equality;

public class EqualityMemberContract : EqualityContract, IEqualityMemberContract
{
    private readonly Type memberType;

    public int? Order { get; set; }

    private protected EqualityMemberContract(Type memberType)
        : base(memberType)
    {
        this.memberType = memberType;
    }

    internal static EqualityMemberContract For(Type memberType)
    {
        return (EqualityMemberContract)Activator.CreateInstance(typeof(EqualityMemberContract<>).MakeGenericType(memberType))!;
    }

    public static IEquatableMemberDescriptor FallbackDescriptorFor(MemberInfo member)
    {
        return new EquatableMemberDescriptor(
            GetFallbackBehavior(
                member switch
                {
                    FieldInfo field => field.FieldType,
                    PropertyInfo property => property.PropertyType,
                    _ => throw new UnreachableException("Unexpected member"),
                }
            ),
            null
        );
    }

    public IEquatableMemberDescriptor ToDescriptor()
    {
        return ChosenBehavior switch
        {
            EqualityBehavior.Comparer => new ComparerEquatableMemberDescriptor(ComparerType, ComparerMember, ComparerArgs, Order),
            EqualityBehavior.Proxy => new ProxyEquatableMemberDescriptor(ProxyType, ProxyMember, ProxyArgs, Order),
            { } behavior => new EquatableMemberDescriptor(behavior, Order),
            null => throw new InvalidOperationException($"{nameof(ChosenBehavior)} is unset"),
        };
    }

    private record EquatableMemberDescriptor(EqualityBehavior Behavior, int? Order) : IEquatableMemberDescriptor;

    private sealed record ComparerEquatableMemberDescriptor(Type ComparerType, string? ComparerMember, object?[] ComparerArgs, int? Order)
        : EquatableMemberDescriptor(EqualityBehavior.Comparer, Order), IComparerEquatableMemberDescriptor;

    private sealed record ProxyEquatableMemberDescriptor(Type ProxyType, string? ProxyMember, object?[] ProxyArgs, int? Order)
        : EquatableMemberDescriptor(EqualityBehavior.Proxy, Order), IProxyEquatableMemberDescriptor;
}

public sealed class EqualityMemberContract<T> : EqualityMemberContract
{
    public EqualityMemberContract()
        : base(typeof(T)) { }
}
