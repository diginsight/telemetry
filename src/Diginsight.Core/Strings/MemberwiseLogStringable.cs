using System.Reflection;

namespace Diginsight.Strings;

public sealed class MemberwiseLogStringable : ReflectionLogStringable
{
    private readonly ILogStringTypeContractAccessor contractAccessor;

    public MemberwiseLogStringable(
        object obj,
        IReflectionLogStringHelper helper,
        ILogStringTypeContractAccessor contractAccessor
    )
        : base(obj, helper, contractAccessor is ILogStringTypeContract)
    {
        this.contractAccessor = contractAccessor;
    }

    protected override LogStringAppender[] MakeAppenders(Type type)
    {
        IEnumerable<(LogStringAppender, int)> MakeAppendersWithOrder<TMember>(
            IEnumerable<TMember> members,
            Func<TMember, bool> isUnreadable,
            Func<TMember, bool> isPublic,
            Func<TMember, Func<object, object?>> getGetValue
        )
            where TMember : MemberInfo
        {
            foreach (TMember member in members)
            {
                if (isUnreadable(member))
                    continue;

                ILogStringMemberContract memberContract = FindMemberContract();

                ILogStringMemberContract FindMemberContract()
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

                    return LogStringMemberContract.Empty;
                }

                bool? included = memberContract.Included;
                if (included == false || included == null && member.IsDefined(typeof(NonLogStringableMemberAttribute)))
                    continue;

                ILogStringableMemberDescriptor? attribute = member.GetCustomAttribute<LogStringableMemberAttribute>();
                if (!(included == true || attribute is not null || isPublic(member)))
                    continue;

                LogStringAppender appender = MakeAppender(
                    memberContract.Name ?? attribute?.Name ?? member.Name,
                    memberContract.ProviderType is { } cpt ? (cpt, memberContract.ProviderArgs)
                    : attribute?.ProviderType is { } apt ? (apt, attribute.ProviderArgs)
                    : null,
                    getGetValue(member)
                );

                yield return (appender, memberContract.Order ?? attribute?.Order ?? 0);
            }
        }

        IEnumerable<(LogStringAppender Appender, int Order)> fieldAppendersWithOrder = MakeAppendersWithOrder(
            type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            static f => f.FieldType.CannotCustomizeLogString(),
            static f => f.IsPublic,
            static f => f.GetValue
        );
        IEnumerable<(LogStringAppender Appender, int Order)> propertyAppendersWithOrder = MakeAppendersWithOrder(
            type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            static p => p.PropertyType.CannotCustomizeLogString() || p.GetMethod is null || p.GetIndexParameters().Length != 0,
            static p => p.GetMethod!.IsPublic,
            static p => p.GetValue
        );
        return fieldAppendersWithOrder.Concat(propertyAppendersWithOrder)
            .OrderByDescending(static x => x.Order)
            .Select(static x => x.Appender)
            .ToArray();
    }

    protected override AllottingCounter Count(AppendingContext appendingContext) => appendingContext.CountMemberwiseProperties();
}
