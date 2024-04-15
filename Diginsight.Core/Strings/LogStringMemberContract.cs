using System.Diagnostics;

namespace Diginsight.Strings;

public class LogStringMemberContract : ILogStringMemberContract
{
    public static readonly ILogStringMemberContract Empty = new LogStringMemberContract();

    private readonly Type? memberType;

    protected Type? providerType;
    protected object[]? providerArgs;

    public bool? Included { get; set; }

    public string? Name { get; set; }

    public Type? ProviderType
    {
        get => providerType;
        set
        {
            if (value is not null && !typeof(ILogStringProvider).IsAssignableFrom(value))
            {
                throw new ArgumentException($"Type '{value.Name}' is not assignable to {nameof(ILogStringProvider)}");
            }

            providerType = value;
        }
    }

    public object[] ProviderArgs
    {
        get => providerArgs ??= [ ];
        set => providerArgs = value;
    }

    public int? Order { get; set; }

    private LogStringMemberContract()
    {
        memberType = null;
    }

    private protected LogStringMemberContract(Type memberType)
    {
        this.memberType = memberType;
    }

    internal static LogStringMemberContract For(Type memberType)
    {
        return (LogStringMemberContract)Activator.CreateInstance(typeof(LogStringMemberContract<>).MakeGenericType(memberType))!;
    }

    public LogStringMemberContract WithCustomTypeContract(Action<LogStringTypeContract> configureContract)
    {
        LogStringTypeContract typeContract = LogStringTypeContract.For(memberType ?? throw new UnreachableException("Dummy member contract"));
        configureContract(typeContract);

        providerType = typeof(CustomMemberwiseLogStringProvider);
        providerArgs = [ typeContract ];

        return this;
    }
}

public sealed class LogStringMemberContract<T> : LogStringMemberContract
{
    public LogStringMemberContract()
        : base(typeof(T)) { }

    public LogStringMemberContract<T> WithCustomTypeContract(Action<LogStringTypeContract<T>> configureContract)
    {
        LogStringTypeContract<T> typeContract = new LogStringTypeContract<T>();
        configureContract(typeContract);

        providerType = typeof(CustomMemberwiseLogStringProvider);
        providerArgs = [ typeContract ];

        return this;
    }
}
