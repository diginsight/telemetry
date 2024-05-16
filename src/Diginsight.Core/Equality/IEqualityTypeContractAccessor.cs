namespace Diginsight.Equality;

public interface IEqualityTypeContractAccessor
{
    IEqualityTypeContract? TryGet(Type type);
}
