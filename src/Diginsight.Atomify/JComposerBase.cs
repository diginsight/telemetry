#if EXPERIMENT_ATOMIFY
namespace Diginsight.Atomify;

/// <summary>
/// Represents a base class for JSON atom composers.
/// </summary>
public abstract class JComposerBase : IJComposer
{
    /// <inheritdoc />
    public bool IsUsed { get; private set; }

    /// <summary>
    /// Marks the composer as used.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the composer has already been used.</exception>
    protected void SetUsed()
    {
        if (IsUsed)
        {
            throw new InvalidOperationException("Composer already used");
        }
        IsUsed = true;
    }
}
#endif
