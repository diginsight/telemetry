using System.Diagnostics;

namespace Diginsight.Strings;

public sealed class LogStringMemberContract : ILogStringMemberContract
{
    public static readonly ILogStringMemberContract Empty = new LogStringMemberContract();

    private readonly Type? memberType;
    private Type? providerType;
    private object[]? providerArgs;

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
        get => providerArgs ??= Array.Empty<object>();
        set => providerArgs = value;
    }

    public int? Order { get; set; }

    private LogStringMemberContract()
    {
        memberType = null;
    }

    internal LogStringMemberContract(Type memberType)
    {
        this.memberType = memberType;
    }

    public LogStringMemberContract WithCustomTypeContract(Action<LogStringTypeContract> configureContract)
    {
        LogStringTypeContract typeContract = new (memberType ?? throw new UnreachableException("Dummy member contract"));
        configureContract(typeContract);

        providerType = typeof(CustomMemberwiseLogStringProvider);
        providerArgs = new object[] { typeContract };

        return this;
    }
}
