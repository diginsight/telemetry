namespace Diginsight.Runtime;

public interface IHeuristicSizeProvider
{
    bool TryGetSizeHeuristically(object obj, HeuristicSizeGetter innerGet, out HeuristicSizeResult result);
}
