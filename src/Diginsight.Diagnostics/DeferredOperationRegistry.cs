using System.Collections.Concurrent;

namespace Diginsight.Diagnostics;

public sealed class DeferredOperationRegistry : IDisposable
{
    private readonly ConcurrentDictionary<IDisposable, ValueTuple> disposables = new ();
    private ConcurrentQueue<IDeferredOperation> operations = new ();
    private volatile bool disposed;

    public void Enqueue(IDeferredOperation operation)
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(DeferredOperationRegistry));

        operations.Enqueue(operation);
    }

    public void Flush(Func<IDeferredOperation, bool> prepareFlush)
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(DeferredOperationRegistry));

        CoreFlush(prepareFlush);
    }

    public void AddDisposable(IDisposable disposable)
    {
        disposables[disposable] = default;
    }

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