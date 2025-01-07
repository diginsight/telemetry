namespace Diginsight;

/// <summary>
/// Represents a disposable object that does nothing when disposed.
/// </summary>
public readonly struct NullDisposable : IDisposable, IAsyncDisposable
{
    /// <inheritdoc />
    public void Dispose() { }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => default;
}
