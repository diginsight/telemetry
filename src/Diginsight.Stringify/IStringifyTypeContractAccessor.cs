namespace Diginsight.Stringify;

public interface IStringifyTypeContractAccessor
{
    IStringifyTypeContract? TryGet(Type type);
}
