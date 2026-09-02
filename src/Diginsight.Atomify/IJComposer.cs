#if EXPERIMENT_ATOMIFY
namespace Diginsight.Atomify;

/// <summary>
/// Represents an interface for composing JSON atoms.
/// </summary>
public interface IJComposer
{
    /// <summary>
    /// Gets a value indicating whether the composer has been used.
    /// </summary>
    bool IsUsed { get; }
}
#endif
