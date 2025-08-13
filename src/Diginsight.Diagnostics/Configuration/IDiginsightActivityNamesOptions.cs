namespace Diginsight.Diagnostics;

public interface IDiginsightActivityNamesOptions
{
    IReadOnlyDictionary<string, LogBehavior> LoggedActivityNames { get; }

    IReadOnlyDictionary<string, bool> SpanMeasuredActivityNames { get; }

    //IEnumerable<string> MetricTags { get; }
}
