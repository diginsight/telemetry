namespace Diginsight;

public interface ISizeableHeuristically
{
    HeuristicSizeResult GetSizeHeuristically(HeuristicSizeGetter innerGet);

    public delegate HeuristicSizeResult HeuristicSizeGetter(object? obj);
}
