#if EXPERIMENT_ATOMIFY
namespace Diginsight.Atomify;

/// <summary>
/// Represents an interface for composing JSON objects.
/// </summary>
public interface IJObjectComposer : IJContainerComposer
{
    /// <summary>
    /// Adds a property to the JSON object with the specified value composer.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="makeValue">The action that composes the property value.</param>
    /// <returns>The current JSON object composer.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the property value composer is not used.</exception>
    IJObjectComposer Property(string name, Action<IJTokenComposer> makeValue);
}
#endif
