namespace Diginsight.Runtime;

public interface ISizeableHeuristically
{
    HeuristicSizeResult GetSizeHeuristically(HeuristicSizeGetter innerGet);
}
