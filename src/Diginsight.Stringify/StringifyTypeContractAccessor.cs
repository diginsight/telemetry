namespace Diginsight.Stringify;

public sealed class StringifyTypeContractAccessor : IStringifyTypeContractAccessor
{
    private readonly IDictionary<Type, StringifyTypeContract> contracts = new Dictionary<Type, StringifyTypeContract>();

    public StringifyTypeContractAccessor()
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

    public StringifyTypeContract GetOrAdd(Type type)
    {
        if (contracts.TryGetValue(type, out StringifyTypeContract? contract))
        {
            return contract;
        }

        if (type.IsForbidden())
        {
            throw new ArgumentException($"Type {type.Name} is forbidden");
        }

        return contracts[type] = StringifyTypeContract.For(type);
    }

    public IStringifyTypeContract? TryGet(Type type)
    {
        return contracts.TryGetValue(type, out StringifyTypeContract? contract) ? contract : null;
    }
}
