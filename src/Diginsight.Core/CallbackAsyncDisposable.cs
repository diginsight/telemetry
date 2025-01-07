namespace Diginsight;

/// <summary>
/// Represents an asynchronous disposable object that executes a callback when disposed.
/// </summary>
public sealed class CallbackAsyncDisposable : IAsyncDisposable
{
    private readonly Func<ValueTask> actionAsync;

    /// <summary>
    /// Initializes a new instance of the <see cref="CallbackAsyncDisposable" /> class.
    /// </summary>
    /// <param name="actionAsync">The asynchronous action to execute when disposing.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="actionAsync" /> is <c>null</c>.</exception>
    public CallbackAsyncDisposable(Func<ValueTask> actionAsync)
    {
        this.actionAsync = actionAsync ?? throw new ArgumentNullException(nameof(actionAsync));
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => actionAsync();
}
