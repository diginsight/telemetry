namespace Diginsight.Diagnostics;

/// <summary>
/// Represents an interface for a deferred operation.
/// </summary>
public interface IDeferredOperation
{
    /// <summary>
    /// Gets a value indicating whether the operation is ready to be flushed.
    /// </summary>
    bool IsFlushable { get; }

    /// <summary>
    /// Flushes the deferred operation.
    /// </summary>
    void Flush();

    /// <summary>
    /// Discards the deferred operation.
    /// </summary>
    void Discard();
}
