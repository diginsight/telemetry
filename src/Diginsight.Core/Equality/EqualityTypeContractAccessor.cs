namespace Diginsight.Equality;

public sealed class EqualityTypeContractAccessor : IEqualityTypeContractAccessor
{
    private readonly IDictionary<Type, EqualityTypeContract> contracts = new Dictionary<Type, EqualityTypeContract>();

    public EqualityTypeContract GetOrAdd(Type type)
    {
        if (contracts.TryGetValue(type, out EqualityTypeContract? contract))
        {
            return contract;
        }

        // TODO Type validation

        return contracts[type] = EqualityTypeContract.For(type);
    }

    public IEqualityTypeContract? TryGet(Type type)
    {
        return contracts.TryGetValue(type, out EqualityTypeContract? contract) ? contract : null;
    }
}
