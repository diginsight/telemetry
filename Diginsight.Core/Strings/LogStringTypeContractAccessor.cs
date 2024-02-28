namespace Diginsight.Strings;

public sealed class LogStringTypeContractAccessor : ILogStringTypeContractAccessor
{
    private readonly IDictionary<Type, LogStringTypeContract> contracts = new Dictionary<Type, LogStringTypeContract>();

    public LogStringTypeContractAccessor()
    {
        this.GetOrAdd<Exception>(
            static tc =>
            {
                tc
                    .GetOrAdd(static x => x.TargetSite, static mc => { mc.Included = false; })
                    .GetOrAdd(static x => x.Data, static mc => { mc.Included = false; })
                    .GetOrAdd(static x => x.HelpLink, static mc => { mc.Included = false; });
            }
        );
    }

    public LogStringTypeContract GetOrAdd(Type type)
    {
        if (contracts.TryGetValue(type, out LogStringTypeContract? contract0))
        {
            return contract0;
        }

        if (type.IsForbidden())
        {
            throw new ArgumentException($"Type {type.Name} is forbidden");
        }

        return contracts[type] = LogStringTypeContract.For(type);
    }

    public ILogStringTypeContract? TryGet(Type type)
    {
        return contracts.TryGetValue(type, out LogStringTypeContract? contract) ? contract : null;
    }
}
