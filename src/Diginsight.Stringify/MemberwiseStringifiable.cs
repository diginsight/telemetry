using System.Reflection;

namespace Diginsight.Stringify;

public sealed class MemberwiseStringifiable : ReflectionStringifiable
{
    private readonly IStringifyTypeContractAccessor contractAccessor;

    public MemberwiseStringifiable(
        object obj,
        IReflectionStringifyHelper helper,
        IStringifyTypeContractAccessor contractAccessor
    )
        : base(obj, helper, contractAccessor is IStringifyTypeContract)
    {
        this.contractAccessor = contractAccessor;
    }

    protected override StringifyAppender[] MakeAppenders(Type type)
    {
        IEnumerable<(StringifyAppender, int)> MakeAppendersWithOrder<TMember>(
            IEnumerable<TMember> members, Func<TMember, bool> isUnreadable, Func<TMember, bool> isPublic
        )
            where TMember : MemberInfo
        {
            foreach (TMember member in members)
            {
                if (isUnreadable(member))
                    continue;

                IStringifyMemberContract memberContract = FindMemberContract();

                IStringifyMemberContract FindMemberContract()
                {
                    foreach (Type t in type.GetClosure())
                    {
                        if (contractAccessor.TryGet(t) is { } tc &&
                            tc.TryGet(member) is { } mc)
                        {
                            return mc;
                        }

                        if (member.DeclaringType == t)
                            break;
                    }

                    return StringifyMemberContract.Empty;
                }

                bool? included = memberContract.Included;
                if (included == false || included == null && member.IsDefined(typeof(NonStringifiableMemberAttribute)))
                    continue;

                IStringifiableMemberDescriptor? attribute = member.GetCustomAttribute<StringifiableMemberAttribute>();
                if (!(included == true || attribute is not null || isPublic(member)))
                    continue;

                StringifyAppender appender = MakeAppender(
                    member,
                    memberContract.Name ?? attribute?.Name,
                    memberContract.StringifierType is { } cpt ? (cpt, memberContract.StringifierArgs)
                    : attribute?.StringifierType is { } apt ? (apt, attribute.StringifierArgs)
                    : null
                );

                yield return (appender, memberContract.Order ?? attribute?.Order ?? 0);
            }
        }

        IEnumerable<(StringifyAppender Appender, int Order)> fieldAppendersWithOrder = MakeAppendersWithOrder(
            type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            static f => f.FieldType.IsForbidden(),
            static f => f.IsPublic
        );
        IEnumerable<(StringifyAppender Appender, int Order)> propertyAppendersWithOrder = MakeAppendersWithOrder(
            type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            static p => p.PropertyType.IsForbidden() || p.GetMethod is null || p.GetIndexParameters().Length != 0,
            static p => p.GetMethod!.IsPublic
        );
        return fieldAppendersWithOrder.Concat(propertyAppendersWithOrder)
            .OrderByDescending(static x => x.Order)
            .Select(static x => x.Appender)
            .ToArray();
    }

    protected override AllottedCounter Count(StringifyContext stringifyContext) => stringifyContext.CountMemberwiseProperties();
}
