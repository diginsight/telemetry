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
        IEnumerable<LogStringAppender> MakeAppendersCore<TMember>(
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

                LogStringableMemberAttribute? attribute = member.GetCustomAttribute<LogStringableMemberAttribute>();
                if (!(included == true || attribute is not null || isPublic(member)))
                    continue;

                yield return MakeAppender(
                    memberContract.Name ?? attribute?.Name ?? member.Name,
                    memberContract.ProviderType ?? attribute?.ProviderType,
                    getGetValue(member)
                );
            }
        }

        IEnumerable<LogStringAppender> fieldAppenders = MakeAppendersCore(
            type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            static f => f.FieldType.IsForbidden(),
            static f => f.IsPublic,
            static f => f.GetValue
        );
        IEnumerable<LogStringAppender> propertyAppenders = MakeAppendersCore(
            type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            static p => p.PropertyType.IsForbidden() || p.GetMethod is null || p.GetIndexParameters().Length != 0,
            static p => p.GetMethod!.IsPublic,
            static p => p.GetValue
        );
        return fieldAppenders.Concat(propertyAppenders).ToArray();
    }

    protected override AllottingCounter Count(LoggingContext loggingContext) => loggingContext.CountMemberwiseProperties();
}
