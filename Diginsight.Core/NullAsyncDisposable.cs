namespace Diginsight;

public sealed class NullAsyncDisposable : IAsyncDisposable
{
    public static readonly IAsyncDisposable Instance = new NullAsyncDisposable();

    private NullAsyncDisposable() { }

    public ValueTask DisposeAsync() => default;

    public void Dispose() { }
}
