using System.Collections.Concurrent;

namespace Diginsight.Diagnostics;

public sealed class DeferredOperationRegistry
{
    private ConcurrentQueue<IDeferredOperation> operations = new ();

    public void Enqueue(IDeferredOperation operation)
    {
        operations.Enqueue(operation);
    }

    public void Flush(Func<IDeferredOperation, bool> prepareFlush)
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

public interface IDeferredOperation
{
    bool IsFlushable { get; }

    void Flush();
}
