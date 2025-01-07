namespace Diginsight.Runtime;

/// <summary>
/// Interface for objects that can provide a heuristic size.
/// </summary>
public interface ISizeableHeuristically
{
    /// <summary>
    /// Gets the size of the object heuristically.
    /// </summary>
    /// <param name="innerGet">The function to get the heuristic size of inner fields or properties.</param>
    /// <returns>A <see cref="HeuristicSizeResult" /> representing the heuristic size.</returns>
    HeuristicSizeResult GetSizeHeuristically(HeuristicSizeGetter innerGet);
}
