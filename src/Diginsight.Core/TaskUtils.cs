using System.Runtime.CompilerServices;

namespace Diginsight;

public static class TaskUtils
{
    // ReSharper disable once AsyncApostle.AsyncMethodNamingHighlighting
    public static Task WhenAnyValid(
        IEnumerable<Func<CancellationToken, Task>> taskFactories,
        int prefetchCount,
        int maxParallelism,
        TimeSpan? maxDelay = default,
        Func<Task, ValueTask<bool>>? isValid = default,
        CancellationToken cancellationToken = default
    )
    {
        return WhenAnyValid<ValueTuple>(
            taskFactories.Select(
                static tf =>
                    (Func<CancellationToken, Task<ValueTuple>>)(ct =>
                        Task.Run(
                            async () =>
                            {
                                await tf(ct);
                                return default(ValueTuple);
                            },
                            ct
                        ))
            ),
            prefetchCount,
            maxParallelism,
            maxDelay,
            isValid,
            cancellationToken
        );
    }

    // ReSharper disable once AsyncApostle.AsyncMethodNamingHighlighting
    public static Task<T> WhenAnyValid<T>(
        IEnumerable<Func<CancellationToken, Task<T>>> taskFactories,
        int prefetchCount,
        int maxParallelism,
        TimeSpan? maxDelay = default,
        Func<Task<T>, ValueTask<bool>>? isValid = default,
        CancellationToken cancellationToken = default
    )
    {
        if (!taskFactories.Any())
        {
            throw new ArgumentException("No tasks provided", nameof(taskFactories));
        }

        ValidateArgumentsOfWhenAnyValid(prefetchCount, maxParallelism, maxDelay);

        SemaphoreSlim semaphore = new (maxParallelism, maxParallelism);

        TaskCompletionSource<T> taskCompletionSource = new (TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenSource ownCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        RunAndForget(
            async () =>
            {
                CancellationToken ownCancellationToken = ownCancellationTokenSource.Token;

                async ValueTask BodyAsync(Func<CancellationToken, Task<T>> taskFactory, CancellationToken innerCancellationToken)
                {
                    await semaphore.WaitAsync(innerCancellationToken);
                    try
                    {
                        Task<T> task = taskFactory(innerCancellationToken);
                        Task winner = await Task.WhenAny(task, Task.Delay(maxDelay ?? Timeout.InfiniteTimeSpan, innerCancellationToken));

                        if (winner != task)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                taskCompletionSource.TrySetCanceled(cancellationToken);
                            }

                            return;
                        }

                        bool valid;
                        if (isValid != null)
                        {
                            try
                            {
                                valid = await isValid(task);
                            }
                            catch (Exception)
                            {
                                return;
                            }
                        }
                        else
                        {
                            valid = task.Status == TaskStatus.RanToCompletion;
                        }

                        if (valid)
                        {
                            ownCancellationTokenSource.Cancel();

                            try
                            {
                                T result = await task;
                                taskCompletionSource.TrySetResult(result);
                            }
                            catch (OperationCanceledException e) when (task.IsCanceled)
                            {
                                taskCompletionSource.TrySetCanceled(e.CancellationToken);
                            }
                            catch (Exception e)
                            {
                                taskCompletionSource.TrySetException(e);
                            }
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }

#if NET
                await Parallel.ForEachAsync(
                    taskFactories,
                    new ParallelOptions() { CancellationToken = ownCancellationToken, MaxDegreeOfParallelism = prefetchCount },
                    BodyAsync
                );
#else
                await Task.WhenAll(taskFactories.Select(async taskFactory => await BodyAsync(taskFactory, ownCancellationToken)));
#endif

                if (!ownCancellationToken.IsCancellationRequested)
                {
                    taskCompletionSource.TrySetException(new InvalidOperationException("No task was valid"));
                }
            }
        );

        return taskCompletionSource.Task;
    }

    private static void ValidateArgumentsOfWhenAnyValid(int prefetchCount, int maxParallelism, TimeSpan? maxDelay)
    {
        if (prefetchCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(prefetchCount), $"{nameof(prefetchCount)} must be positive");
        if (maxParallelism <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxParallelism), $"{nameof(maxParallelism)} must be positive");
        if (maxParallelism > prefetchCount)
            throw new ArgumentException($"{nameof(maxParallelism)} must be less than or equal to {nameof(prefetchCount)}");
        if (maxDelay is { } maxDelay0 && maxDelay0 < TimeSpan.Zero && maxDelay0 != Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(maxDelay), $"{nameof(maxDelay)} must be null, positive or infinite");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RunAndForget(Action action, CancellationToken cancellationToken = default)
    {
        _ = Task.Run(action, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RunAndForget<T>(Func<T> func, CancellationToken cancellationToken = default)
    {
        _ = Task.Run(func, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RunAndForget(Func<Task> actionAsync, CancellationToken cancellationToken = default)
    {
        _ = Task.Run(actionAsync, cancellationToken);
    }
}
