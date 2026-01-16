using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Diginsight;

/// <summary>
/// Provides utility methods for working with tasks.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TaskUtils
{
    /// <summary>
    /// Executes a collection of tasks and returns as soon as the first valid task completes.
    /// </summary>
    /// <param name="taskFactories">The functions generating the tasks to execute.</param>
    /// <param name="prefetchCount">The number of tasks to prefetch.</param>
    /// <param name="maxParallelism">The maximum degree of parallelism.</param>
    /// <param name="maxDelay">The maximum delay for task completion.</param>
    /// <param name="isValid">A function to determine whether a completed task is valid.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the completion of any valid task.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="taskFactories" /> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any of the following is true:
    /// <list type="bullet">
    ///     <item>
    ///         <description><paramref name="prefetchCount" /> is not positive.</description>
    ///     </item>
    ///     <item>
    ///         <description><paramref name="maxParallelism" /> is not positive.</description>
    ///     </item>
    ///     <item>
    ///         <description><paramref name="maxParallelism" /> is greater than <paramref name="prefetchCount" />.</description>
    ///     </item>
    /// </list>
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when no task completes validly.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken" /> is canceled.</exception>
    /// <remarks>
    ///     <para>If <paramref name="maxDelay" /> is <c>null</c> or <see cref="Expiration.Never" />, task execution is not time-bound.</para>
    ///     <para>If <paramref name="isValid" /> is <c>null</c>, tasks are considered valid if <see cref="Task.Status" /> is <see cref="TaskStatus.RanToCompletion" />.</para>
    /// </remarks>
    // ReSharper disable once AsyncApostle.AsyncMethodNamingHighlighting
    public static Task WhenAnyValid(
        IEnumerable<Func<CancellationToken, Task>> taskFactories,
        int prefetchCount,
        int maxParallelism,
        Expiration? maxDelay,
        Func<Task, ValueTask<bool>>? isValid = null,
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

    /// <summary>
    /// Executes a collection of tasks and returns the value produced by the first valid complete task.
    /// </summary>
    /// <param name="taskFactories">The functions generating the tasks to execute.</param>
    /// <param name="prefetchCount">The number of tasks to prefetch.</param>
    /// <param name="maxParallelism">The maximum degree of parallelism.</param>
    /// <param name="maxDelay">The maximum delay for task completion.</param>
    /// <param name="isValid">A function to determine whether a completed task is valid.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the completion of any valid task, together with its result.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="taskFactories" /> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any of the following is true:
    /// <list type="bullet">
    ///     <item>
    ///         <description><paramref name="prefetchCount" /> is not positive.</description>
    ///     </item>
    ///     <item>
    ///         <description><paramref name="maxParallelism" /> is not positive.</description>
    ///     </item>
    ///     <item>
    ///         <description><paramref name="maxParallelism" /> is greater than <paramref name="prefetchCount" />.</description>
    ///     </item>
    /// </list>
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when no task completes validly.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken" /> is canceled.</exception>
    /// <remarks>
    ///     <para>If <paramref name="maxDelay" /> is <c>null</c> or <see cref="Expiration.Never" />, task execution is not time-bound.</para>
    ///     <para>If <paramref name="isValid" /> is <c>null</c>, tasks are considered valid if <see cref="Task.Status" /> is <see cref="TaskStatus.RanToCompletion" />.</para>
    /// </remarks>
    // ReSharper disable once AsyncApostle.AsyncMethodNamingHighlighting
    public static Task<T> WhenAnyValid<T>(
        IEnumerable<Func<CancellationToken, Task<T>>> taskFactories,
        int prefetchCount,
        int maxParallelism,
        Expiration? maxDelay,
        Func<Task<T>, ValueTask<bool>>? isValid = null,
        CancellationToken cancellationToken = default
    )
    {
        if (!taskFactories.Any())
        {
            throw new ArgumentException("No tasks provided", nameof(taskFactories));
        }

        Expiration finalMaxDelay = maxDelay ?? Expiration.Never;
        ValidateArgumentsOfWhenAnyValid(prefetchCount, maxParallelism);

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
                        Task winner = await Task.WhenAny(
                            task,
                            Task.Delay(finalMaxDelay.IsNever ? Timeout.InfiniteTimeSpan : finalMaxDelay.Value, innerCancellationToken)
                        );

                        if (winner != task)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                // ReSharper disable once PossiblyMistakenUseOfCancellationToken
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
            },
            CancellationToken.None
        );

        return taskCompletionSource.Task;
    }

    private static void ValidateArgumentsOfWhenAnyValid(int prefetchCount, int maxParallelism)
    {
        if (prefetchCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(prefetchCount), $"{nameof(prefetchCount)} must be positive");
        if (maxParallelism <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxParallelism), $"{nameof(maxParallelism)} must be positive");
        if (maxParallelism > prefetchCount)
            throw new ArgumentException($"{nameof(maxParallelism)} must be less than or equal to {nameof(prefetchCount)}");
    }

    /// <summary>
    /// Runs an action in a separate async flow without waiting for completion.
    /// </summary>
    /// <param name="action">The action to run.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RunAndForget(Action action, CancellationToken cancellationToken = default)
    {
        _ = Task.Run(action, cancellationToken);
    }

    /// <summary>
    /// Runs a function in a separate async flow without waiting for completion.
    /// </summary>
    /// <typeparam name="T">The type of the function result.</typeparam>
    /// <param name="func">The function to run.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RunAndForget<T>(Func<T> func, CancellationToken cancellationToken = default)
    {
        _ = Task.Run(func, cancellationToken);
    }

    /// <summary>
    /// Runs an asynchronous action in a separate async flow without waiting for completion.
    /// </summary>
    /// <param name="actionAsync">The asynchronous action to run.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RunAndForget(Func<Task> actionAsync, CancellationToken cancellationToken = default)
    {
        _ = Task.Run(actionAsync, cancellationToken);
    }
}
