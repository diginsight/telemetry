using System.Diagnostics;
using System.Reflection;

namespace Diginsight.Strings;

public sealed class LogStringTypeContract : ILogStringTypeContract
{
    private readonly Type type;
    private readonly IDictionary<MemberInfo, LogStringMemberContract> memberContracts = new Dictionary<MemberInfo, LogStringMemberContract>();

    public LogStringTypeContract(Type type)
    {
        this.type = type;
    }

    public bool? Included { get; set; }

    public LogStringMemberContract GetOrAdd(string memberName)
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

    public LogStringMemberContract GetOrAdd(MemberInfo member)
    {
        return GetOrAddCore(member, true);
    }

    private LogStringMemberContract GetOrAddCore(MemberInfo member, bool validateMembership)
    {
        if (memberContracts.TryGetValue(member, out LogStringMemberContract? memberContract))
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

        return memberContracts[member] = new LogStringMemberContract();
    }

    public ILogStringMemberContract? TryGet(MemberInfo member)
    {
        return memberContracts.TryGetValue(member, out LogStringMemberContract? memberContract) ? memberContract : null;
    }
}
