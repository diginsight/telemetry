using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Diginsight.Equality;

public class EqualityTypeContract : IEqualityTypeContract
{
    private readonly Type type;
    private readonly IDictionary<MemberInfo, EqualityMemberContract> memberContracts =
        new Dictionary<MemberInfo, EqualityMemberContract>(MetadataMemberInfoEqualityComparer.Instance);

    protected object[]? proxyArgs;

    private sealed class MetadataMemberInfoEqualityComparer : IEqualityComparer<MemberInfo>
    {
        public static readonly IEqualityComparer<MemberInfo> Instance = new MetadataMemberInfoEqualityComparer();

        private MetadataMemberInfoEqualityComparer() { }

        public bool Equals(MemberInfo? o1, MemberInfo? o2)
        {
            if (o1 == o2)
                return true;
            if (o1 is null || o2 is null)
                return false;
            return o1.HasSameMetadataDefinitionAs(o2);
        }

        public int GetHashCode(MemberInfo obj) => obj.GetHashCode();
    }

    public bool? Included { get; set; }

    public EqualityMode Mode { get; set; }

    public Type? ProxyType { get; set; }

    public object[] ProxyArgs
    {
        get => proxyArgs ??= [ ];
        set => proxyArgs = value;
    }

    private protected EqualityTypeContract(Type type)
    {
        this.type = type;
    }

    public static EqualityTypeContract For(Type type)
    {
        return (EqualityTypeContract)Activator.CreateInstance(typeof(EqualityTypeContract<>).MakeGenericType(type))!;
    }

    public EqualityMemberContract GetOrAdd(string memberName)
    {
        MemberInfo[] candidateMembers = type.FindMembers(
            MemberTypes.Field | MemberTypes.Property,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            static (m, o) => m.Name == (string)o!,
            memberName
        );

        return candidateMembers.Length switch
        {
            0 => throw new ArgumentException($"No such field or property named '{memberName}'"),
            1 => GetOrAddCore(candidateMembers[0], false),
            _ => throw new UnreachableException($"More than one field or property named '{memberName}'"),
        };
    }

    public EqualityMemberContract GetOrAdd(MemberInfo member)
    {
        return GetOrAddCore(member, true);
    }

    protected EqualityMemberContract GetOrAddCore(MemberInfo member, bool validateMembership)
    {
        if (memberContracts.TryGetValue(member, out EqualityMemberContract? memberContract))
        {
            return memberContract;
        }

        string memberName = member.Name;
        Type memberType;
        switch (member)
        {
            case FieldInfo f:
                if (validateMembership && type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != f)
                {
                    throw new ArgumentException($"Field '{memberName}' does not belong to type '{type.Name}'");
                }

                memberType = f.FieldType;
                // TODO Type validation

                break;

            case PropertyInfo p:
                if (validateMembership && type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != p)
                {
                    throw new ArgumentException($"Property '{memberName}' does not belong to type '{type.Name}'");
                }
                if (p.GetMethod is null)
                {
                    throw new ArgumentException($"Property '{memberName}' is not readable");
                }
                if (p.GetIndexParameters().Length != 0)
                {
                    throw new ArgumentException($"Property '{memberName}' is parameterized");
                }

                memberType = p.PropertyType;
                // TODO Type validation

                break;

            default:
                throw new ArgumentException($"Member '{memberName}' is not a field or a property");
        }

        return memberContracts[member] = EqualityMemberContract.For(memberType);
    }

    public IEqualityMemberContract? TryGet(MemberInfo member)
    {
        return memberContracts.TryGetValue(member, out EqualityMemberContract? memberContract) ? memberContract : null;
    }

    // ReSharper disable once ParameterHidesMember
    IEqualityTypeContract? IEqualityTypeContractAccessor.TryGet(Type type)
    {
        return type == this.type ? this : null;
    }
}

public sealed class EqualityTypeContract<T> : EqualityTypeContract
{
    public EqualityTypeContract()
        : base(typeof(T)) { }

    public EqualityMemberContract<TMember> GetOrAdd<TMember>(Expression<Func<T, TMember>> expression)
    {
        if (expression.Body is not MemberExpression bodyExpr)
        {
            throw new ArgumentException("Expression must be a member access expression");
        }
        if (bodyExpr.Expression != expression.Parameters[0])
        {
            throw new ArgumentException("Expression must access a member of the parameter");
        }

        return (EqualityMemberContract<TMember>)GetOrAddCore(bodyExpr.Member, false);
    }
}
