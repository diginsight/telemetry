using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace Diginsight.Strings;

public sealed class LogStringTypeContract
{
    public static readonly LogStringTypeContract Empty = new (null, new Dictionary<MemberInfo, LogStringMemberContract>());

    public bool? Included { get; }
    public IReadOnlyDictionary<MemberInfo, LogStringMemberContract> MemberContracts { get; }

    private LogStringTypeContract(bool? included, IReadOnlyDictionary<MemberInfo, LogStringMemberContract> memberContracts)
    {
        Included = included;
        MemberContracts = memberContracts;
    }

    public static LogStringTypeContract For(Type type, Action<IFluentTypeConfigurator> configureType)
    {
        FluentTypeConfigurator configurator = new (type);
        configureType(configurator);

        return new LogStringTypeContract(
            configurator.Included,
            configurator.Members.ToDictionary(
                static x => x.Key,
                static x => new LogStringMemberContract(x.Value.Included, x.Value.Name, x.Value.ProviderType, false)
            )
        );
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IFluentTypeConfigurator : IFluentMemberTypeConfigurator
    {
        IFluentTypeConfigurator SetIncluded(bool? included);

        new IFluentTypeConfigurator ForMember(string memberName, Action<IFluentMemberConfigurator> configureMember);

        new IFluentTypeConfigurator ForMember(MemberInfo member, Action<IFluentMemberConfigurator> configureMember);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IFluentMemberConfigurator
    {
        IFluentMemberConfigurator SetIncluded(bool? included);

        IFluentMemberConfigurator WithName(string? name);

        IFluentMemberConfigurator WithProvider(Type? providerType);

        IFluentMemberConfigurator WithCustom(Action<IFluentMemberTypeConfigurator> configureType);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IFluentMemberTypeConfigurator
    {
        IFluentMemberTypeConfigurator ForMember(string memberName, Action<IFluentMemberConfigurator> configureMember);

        IFluentMemberTypeConfigurator ForMember(MemberInfo member, Action<IFluentMemberConfigurator> configureMember);
    }

    private sealed class FluentTypeConfigurator : IFluentTypeConfigurator
    {
        private readonly Type type;
        private readonly IDictionary<MemberInfo, FluentMemberConfigurator> members = new Dictionary<MemberInfo, FluentMemberConfigurator>();

        public bool? Included { get; private set; }
        public IEnumerable<KeyValuePair<MemberInfo, FluentMemberConfigurator>> Members => members;

        public FluentTypeConfigurator(Type type)
        {
            this.type = type;
        }

        public IFluentTypeConfigurator SetIncluded(bool? included)
        {
            Included = included;
            return this;
        }

        public IFluentTypeConfigurator ForMember(string memberName, Action<IFluentMemberConfigurator> configureMember)
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
                1 => ForMemberCore(candidateMembers[0], configureMember, false),
                _ => throw new UnreachableException($"More than one field or property named '{memberName}'"),
            };
        }

        IFluentMemberTypeConfigurator IFluentMemberTypeConfigurator.ForMember(string memberName, Action<IFluentMemberConfigurator> configureMember)
        {
            return ForMember(memberName, configureMember);
        }

        public IFluentTypeConfigurator ForMember(MemberInfo member, Action<IFluentMemberConfigurator> configureMember)
        {
            return ForMemberCore(member, configureMember, true);
        }

        IFluentMemberTypeConfigurator IFluentMemberTypeConfigurator.ForMember(MemberInfo member, Action<IFluentMemberConfigurator> configureMember)
        {
            return ForMember(member, configureMember);
        }

        private IFluentTypeConfigurator ForMemberCore(MemberInfo member, Action<IFluentMemberConfigurator> configureMember, bool validateMembership)
        {
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

            if (!members.TryGetValue(member, out FluentMemberConfigurator? configurator))
            {
                members[member] = configurator = new (memberType);
            }

            configureMember(configurator);

            return this;
        }
    }

    private sealed class FluentMemberConfigurator : IFluentMemberConfigurator
    {
        private readonly Type memberType;

        public bool? Included { get; private set; }
        public string? Name { get; private set; }
        public Type? ProviderType { get; private set; }

        public FluentMemberConfigurator(Type memberType)
        {
            this.memberType = memberType;
        }

        public IFluentMemberConfigurator SetIncluded(bool? included)
        {
            Included = included;
            return this;
        }

        public IFluentMemberConfigurator WithName(string? name)
        {
            Name = name;
            return this;
        }

        public IFluentMemberConfigurator WithProvider(Type? providerType)
        {
            if (providerType is not null && !typeof(ILogStringProvider).IsAssignableFrom(providerType))
            {
                throw new ArgumentException($"Type '{providerType.Name}' is not assignable to {nameof(ILogStringProvider)}");
            }

            ProviderType = providerType;
            return this;
        }

        public IFluentMemberConfigurator WithCustom(Action<IFluentMemberTypeConfigurator> configureType)
        {
            throw new NotImplementedException();
        }
    }
}
