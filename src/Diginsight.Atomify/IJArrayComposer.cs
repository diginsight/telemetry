#if EXPERIMENT_ATOMIFY
namespace Diginsight.Atomify;

/// <summary>
/// Represents an interface for composing JSON arrays.
/// </summary>
public interface IJArrayComposer : IJContainerComposer
{
    /// <summary>
    /// Adds an item to the JSON array with the specified value composer.
    /// </summary>
    /// <param name="makeValue">The action that composes the item value.</param>
    /// <returns>The same <see cref="IJArrayComposer" /> instance, for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the item value composer is not used.</exception>
    IJArrayComposer Item(Action<IJTokenComposer> makeValue);
}
#endif
