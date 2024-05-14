namespace Diginsight;

public readonly struct NullDisposable : IDisposable, IAsyncDisposable
{
    public void Dispose() { }

    public ValueTask DisposeAsync() => default;
}
