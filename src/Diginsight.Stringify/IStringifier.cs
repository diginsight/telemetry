namespace Diginsight.Stringify;

public interface IStringifier
{
    IStringifiable? TryStringify(object obj);
}
