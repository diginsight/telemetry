using Microsoft.Extensions.Options;
using System.Collections;
using System.Reflection;
using Appender = System.Action<object, System.Text.StringBuilder, Diginsight.Strings.LoggingContext>;

namespace Diginsight.Strings;

internal sealed class MemberwiseLogStringProvider : ReflectionLogStringProvider
{
    private readonly ILogStringConfiguration logStringConfiguration;
    private readonly IReadOnlyDictionary<Type, LogStringTypeContract> typeContracts;

    private readonly IDictionary<Type, Handling> handlingCache = new Dictionary<Type, Handling>();

    public MemberwiseLogStringProvider(
        IMemberLogStringProvider memberLogStringProvider,
        IServiceProvider serviceProvider,
        IOptions<LogStringConfiguration> logStringConfigurationOptions,
        IOptions<LogStringTypeContractDictionary> typeContractsOptions
    )
        : base(memberLogStringProvider, serviceProvider)
    {
        logStringConfiguration = logStringConfigurationOptions.Value;
        typeContracts = typeContractsOptions.Value;
    }

    protected override Handling IsHandled(Type type)
    {
        lock (((ICollection)handlingCache).SyncRoot)
        {
            return handlingCache.TryGetValue(type, out Handling handling)
                ? handling
                : handlingCache[type] = IsHandledCore();

            Handling IsHandledCore()
            {
                foreach (Type t in GetClosure(type))
                {
                    if (typeContracts.TryGetValue(t, out LogStringTypeContract? typeContract))
                    {
                        switch (typeContract.Included)
                        {
                            case true:
                                return Handling.Handle;

                            case false:
                                return Handling.Forbid;
                        }
                    }

                    if (t.IsDefined(typeof(LogStringableObjectAttribute), false))
                    {
                        return Handling.Handle;
                    }
                    if (t.IsDefined(typeof(NonLogStringableObjectAttribute), false))
                    {
                        return Handling.Forbid;
                    }
                }

                return logStringConfiguration.IsMemberwiseLogStringableByDefault ? Handling.Handle : Handling.Pass;
            }
        }
    }

    protected override Appender[] MakeAppenders(Type type)
    {
        IEnumerable<Appender> MakeAppendersCore<TMember>(
            IEnumerable<TMember> members, Func<TMember, bool> isUnreadable, Func<TMember, bool> isPublic, Func<TMember, Func<object, object?>> getGetValue
        )
            where TMember : MemberInfo
        {
            foreach (TMember member in members)
            {
                if (isUnreadable(member))
                    continue;

                LogStringMemberContract memberContract = FindMemberContract();

                LogStringMemberContract FindMemberContract()
                {
                    foreach (Type t in GetClosure(type))
                    {
                        if (typeContracts.TryGetValue(t, out LogStringTypeContract? tc) &&
                            tc.MemberContracts.TryGetValue(member, out LogStringMemberContract? mc))
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
                    memberContract.ProviderType ?? attribute?.Provider,
                    getGetValue(member)
                );
            }
        }

        IEnumerable<Appender> fieldAppenders = MakeAppendersCore(
            type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            static f => f.FieldType.IsForbidden(),
            static f => f.IsPublic,
            static f => f.GetValue
        );
        IEnumerable<Appender> propertyAppenders = MakeAppendersCore(
            type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            static p => p.PropertyType.IsForbidden() || p.GetMethod is null || p.GetIndexParameters().Length != 0,
            static p => p.GetMethod!.IsPublic,
            static p => p.GetValue
        );
        return fieldAppenders.Concat(propertyAppenders).ToArray();
    }

    private static IEnumerable<Type> GetClosure(Type type)
    {
        Type? currentType = type;
        while (currentType is not null)
        {
            yield return currentType;
            currentType = currentType.BaseType;
        }

        foreach (Type @interface in type.GetInterfaces())
        {
            yield return @interface;
        }
    }

    protected override AllottingCounter Count(LoggingContext loggingContext) => loggingContext.CountMemberwiseProperties();
}
