namespace Diginsight.Runtime;

/// <summary>
/// Delegate for obtaining heuristic size results.
/// </summary>
/// <param name="obj">The object for which to obtain the heuristic size result.</param>
/// <returns>A <see cref="HeuristicSizeResult" /> representing the heuristic size of the object.</returns>
public delegate HeuristicSizeResult HeuristicSizeGetter(object? obj);
