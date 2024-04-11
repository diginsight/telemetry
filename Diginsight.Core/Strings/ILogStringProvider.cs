namespace Diginsight.Strings;

public interface ILogStringProvider
{
    ILogStringable? TryToLogStringable(object obj);
}
