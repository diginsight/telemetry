using System.Collections.Concurrent;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents a registry for deferred logging operations and associated disposables.
/// </summary>
public sealed class DeferredOperationRegistry : IDisposable
{
    private readonly ConcurrentDictionary<IDisposable, ValueTuple> disposables = new ();
    private ConcurrentQueue<IDeferredOperation> operations = new ();
    private volatile bool disposed;

    /// <summary>
    /// Enqueues a deferred operation.
    /// </summary>
    /// <param name="operation">The deferred operation to enqueue.</param>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void Enqueue(IDeferredOperation operation)
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(DeferredOperationRegistry));

        operations.Enqueue(operation);
    }

    /// <summary>
    /// Flushes deferred operations that are ready for execution.
    /// </summary>
    /// <param name="prepareFlush">The function that prepares an operation for flushing.</param>
    /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed.</exception>
    public void Flush(Func<IDeferredOperation, bool> prepareFlush)
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(DeferredOperationRegistry));

        CoreFlush(prepareFlush);
    }

    /// <summary>
    /// Adds a disposable to be disposed with the registry.
    /// </summary>
    /// <param name="disposable">The disposable to add.</param>
    public void AddDisposable(IDisposable disposable)
    {
        disposables[disposable] = default;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;

        CoreFlush(
            static x =>
            {
                x.Discard();
                return true;
            }
        );

        foreach (IDisposable disposable in disposables.Keys)
        {
            disposable.Dispose();
        }
    }

    private void CoreFlush(Func<IDeferredOperation, bool> prepareFlush)
    {
        ConcurrentQueue<IDeferredOperation> newOperations = new ();
        bool flushing = true;

        while (operations.TryDequeue(out IDeferredOperation? operation))
        {
            if (operation.IsFlushable || prepareFlush(operation))
            {
                if (flushing)
                {
                    operation.Flush();
                    continue;
                }
            }
            else
            {
                flushing = false;
            }

            newOperations.Enqueue(operation);
        }

        Interlocked.Exchange(ref operations, newOperations);
    }
}
