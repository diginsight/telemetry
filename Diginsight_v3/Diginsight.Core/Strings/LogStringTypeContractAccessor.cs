namespace Diginsight.Strings;

public sealed class LogStringTypeContractAccessor : ILogStringTypeContractAccessor
{
    private readonly IDictionary<Type, LogStringTypeContract> contracts = new Dictionary<Type, LogStringTypeContract>();

    public LogStringTypeContractAccessor()
    {
        LogStringTypeContract exceptionContract = GetOrAdd(typeof(Exception));
        exceptionContract.GetOrAdd(nameof(Exception.TargetSite)).Included = false;
        exceptionContract.GetOrAdd(nameof(Exception.Data)).Included = false;
        exceptionContract.GetOrAdd(nameof(Exception.HelpLink)).Included = false;
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

    public ILogStringTypeContract? TryGet(Type type)
    {
        return contracts.TryGetValue(type, out LogStringTypeContract? contract) ? contract : null;
    }
}
