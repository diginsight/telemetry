namespace Diginsight.Strings;

public interface ILogStringProvider
{
    ILogStringable? TryAsLogStringable(object obj);
}
