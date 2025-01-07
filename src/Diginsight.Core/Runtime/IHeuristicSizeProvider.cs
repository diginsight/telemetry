namespace Diginsight.Runtime;

/// <summary>
/// Interface for providing heuristic size of an object.
/// </summary>
public interface IHeuristicSizeProvider
{
    /// <summary>
    /// Tries to get the size of the object heuristically.
    /// </summary>
    /// <param name="obj">The object to get the size of.</param>
    /// <param name="innerGet">The function to get the heuristic size of inner fields or properties.</param>
    /// <param name="result">The result of the heuristic size calculation.</param>
    /// <returns><c>true</c> if the provider supports calculating the size for the given object; otherwise, <c>false</c>.</returns>
    bool TryGetSizeHeuristically(object obj, HeuristicSizeGetter innerGet, out HeuristicSizeResult result);
}
