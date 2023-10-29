namespace Diginsight;

public sealed class CallbackDisposable : IDisposable
{
    private readonly Action action;

    public CallbackDisposable(Action action)
    {
        this.action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public void Dispose() => action();
}
