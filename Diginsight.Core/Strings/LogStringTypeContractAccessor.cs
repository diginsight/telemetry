namespace Diginsight.Strings;

public sealed class LogStringTypeContractAccessor : ILogStringTypeContractAccessor
{
    private readonly IDictionary<Type, LogStringTypeContract> contracts = new Dictionary<Type, LogStringTypeContract>();

    public LogStringTypeContractAccessor()
    {
        GetOrAdd(
            typeof(Exception),
            static tc =>
            {
                tc
                    .GetOrAdd(nameof(Exception.TargetSite), static mc => { mc.Included = false; })
                    .GetOrAdd(nameof(Exception.Data), static mc => { mc.Included = false; })
                    .GetOrAdd(nameof(Exception.HelpLink), static mc => { mc.Included = false; });
            }
        );
    }

    public LogStringTypeContract GetOrAdd(Type type)
    {
        if (contracts.TryGetValue(type, out LogStringTypeContract? contract))
        {
            return contract;
        }

        if (type.IsForbidden())
        {
            throw new ArgumentException($"Type {type.Name} is forbidden");
        }

        return contracts[type] = new LogStringTypeContract(type);
    }

    public LogStringTypeContractAccessor GetOrAdd(Type type, Action<LogStringTypeContract> configureContract)
    {
        LogStringTypeContract contract = GetOrAdd(type);
        configureContract(contract);
        return this;
    }

    public ILogStringTypeContract? TryGet(Type type)
    {
        return contracts.TryGetValue(type, out LogStringTypeContract? contract) ? contract : null;
    }
}
