namespace Diginsight.Strings;

public interface ILogStringTypeContractAccessor
{
    ILogStringTypeContract? TryGet(Type type);
}
