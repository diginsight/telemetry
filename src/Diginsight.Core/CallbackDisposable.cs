namespace Diginsight;

/// <summary>
/// Represents a disposable object that executes a callback when disposed.
/// </summary>
public sealed class CallbackDisposable : IDisposable
{
    private readonly Action action;

    /// <summary>
    /// Initializes a new instance of the <see cref="CallbackDisposable" /> class.
    /// </summary>
    /// <param name="action">The action to execute when disposing.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action" /> is <c>null</c>.</exception>
    public CallbackDisposable(Action action)
    {
        this.action = action ?? throw new ArgumentNullException(nameof(action));
    }

    /// <inheritdoc />
    public void Dispose() => action();
}
