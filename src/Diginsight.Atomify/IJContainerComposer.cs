#if EXPERIMENT_ATOMIFY
namespace Diginsight.Atomify;

/// <summary>
/// Represents an interface for composing JSON containers.
/// </summary>
public interface IJContainerComposer : IJComposer
{
    /// <summary>
    /// Ends composition of the current JSON container.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the container composer has already been used.</exception>
    void End();
}
#endif
