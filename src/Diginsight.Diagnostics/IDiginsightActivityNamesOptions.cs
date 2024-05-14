namespace Diginsight.Diagnostics;

public interface IDiginsightActivityNamesOptions
{
    IEnumerable<string> LoggedActivityNames { get; }
    IEnumerable<string> NonLoggedActivityNames { get; }
}
