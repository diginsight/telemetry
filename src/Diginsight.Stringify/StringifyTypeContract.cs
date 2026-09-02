using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Diginsight.Stringify;

/// <summary>
/// Represents configurable stringification rules for a type.
/// </summary>
public class StringifyTypeContract : IStringifyTypeContract
{
    private readonly Type type;

    private readonly IDictionary<MemberInfo, StringifyMemberContract> memberContracts =
        new Dictionary<MemberInfo, StringifyMemberContract>(MetadataMemberInfoEqualityComparer.Instance);

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

    /// <summary>
    /// Gets a value indicating whether the type or member is included in stringification.
    /// </summary>
    public bool? Included { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringifyTypeContract" /> class.
    /// </summary>
    /// <param name="type">The type.</param>
    private protected StringifyTypeContract(Type type)
    {
        this.type = type;
    }

    /// <summary>
    /// Creates a stringify type contract for the specified type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The type contract.</returns>
    public static StringifyTypeContract For(Type type)
    {
        return (StringifyTypeContract)Activator.CreateInstance(typeof(StringifyTypeContract<>).MakeGenericType(type))!;
    }

    /// <summary>
    /// Gets an existing contract or adds a new contract.
    /// </summary>
    /// <param name="memberName">The member name.</param>
    /// <returns>The configured contract accessor or contract.</returns>
    /// <exception cref="ArgumentException">Thrown when the member cannot be used by the contract.</exception>
    public StringifyMemberContract GetOrAdd(string memberName)
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

    /// <summary>
    /// Gets an existing contract or adds a new contract.
    /// </summary>
    /// <param name="member">The member.</param>
    /// <returns>The configured contract accessor or contract.</returns>
    /// <exception cref="ArgumentException">Thrown when the member cannot be used by the contract.</exception>
    public StringifyMemberContract GetOrAdd(MemberInfo member)
    {
        return GetOrAddCore(member, true);
    }

    /// <summary>
    /// Gets an existing member contract or adds a new member contract.
    /// </summary>
    /// <param name="member">The member.</param>
    /// <param name="validateMembership">A value indicating whether membership in the declaring type is validated.</param>
    /// <returns>The member contract.</returns>
    /// <exception cref="ArgumentException">Thrown when the member cannot be used by the contract.</exception>
    protected StringifyMemberContract GetOrAddCore(MemberInfo member, bool validateMembership)
    {
        if (memberContracts.TryGetValue(member, out StringifyMemberContract? memberContract))
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
                if (memberType.IsForbidden())
                {
                    throw new ArgumentException($"Field '{memberName}' has a forbidden type");
                }

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
                if (memberType.IsForbidden())
                {
                    throw new ArgumentException($"Property '{memberName}' has a forbidden type");
                }

                break;

            default:
                throw new ArgumentException($"Member '{memberName}' is not a field or a property");
        }

        return memberContracts[member] = StringifyMemberContract.For(memberType);
    }

    /// <summary>
    /// Gets the member contract associated with the specified member.
    /// </summary>
    /// <param name="member">The member.</param>
    /// <returns>The matching stringify contract if one exists; otherwise, <c>null</c>.</returns>
    public IStringifyMemberContract? TryGet(MemberInfo member)
    {
        return memberContracts.TryGetValue(member, out StringifyMemberContract? memberContract) ? memberContract : null;
    }

    IStringifyTypeContract? IStringifyTypeContractAccessor.TryGet(
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        Type type
    )
    {
        return type == this.type ? this : null;
    }
}

/// <summary>
/// Represents configurable stringification rules for a specific type.
/// </summary>
/// <typeparam name="T">The type.</typeparam>
public sealed class StringifyTypeContract<T> : StringifyTypeContract
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringifyTypeContract" /> class.
    /// </summary>
    public StringifyTypeContract()
        : base(typeof(T)) { }

    /// <summary>
    /// Gets an existing contract or adds a new contract.
    /// </summary>
    /// <typeparam name="TMember">The member type.</typeparam>
    /// <param name="expression">The member access expression.</param>
    /// <returns>The configured contract accessor or contract.</returns>
    /// <exception cref="ArgumentException">Thrown when the member cannot be used by the contract.</exception>
    public StringifyMemberContract<TMember> GetOrAdd<TMember>(Expression<Func<T, TMember>> expression)
    {
        if (expression.Body is not MemberExpression bodyExpr)
        {
            throw new ArgumentException("Expression must be a member access expression");
        }
        if (bodyExpr.Expression != expression.Parameters[0])
        {
            throw new ArgumentException("Expression must access a member of the parameter");
        }

        return (StringifyMemberContract<TMember>)GetOrAddCore(bodyExpr.Member, false);
    }
}
