namespace Diginsight.Diagnostics;

public interface IDiginsightActivityNamesOptions
{
    IEnumerable<string> LoggedActivityNames { get; }
    IEnumerable<string> NonLoggedActivityNames { get; }

    IEnumerable<string> SpanMeasuredActivityNames { get; }
    IEnumerable<string> NonSpanMeasuredActivityNames { get; }
}
