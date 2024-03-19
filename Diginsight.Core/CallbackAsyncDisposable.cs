#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
namespace Diginsight;

public sealed class CallbackAsyncDisposable : IAsyncDisposable
{ 
    private readonly Func<ValueTask> actionAsync;

    public CallbackAsyncDisposable(Func<ValueTask> actionAsync)
    {
        this.actionAsync = actionAsync ?? throw new ArgumentNullException(nameof(actionAsync));
    }

    public ValueTask DisposeAsync() => actionAsync();
}
#endif
