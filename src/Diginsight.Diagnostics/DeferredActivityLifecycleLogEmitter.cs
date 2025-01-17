using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

[SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
public sealed class DeferredActivityLifecycleLogEmitter
{
    private readonly DeferredOperationRegistry operationRegistry;
#if NET9_0_OR_GREATER
    private readonly Lock @lock = new ();
#else
    private readonly object @lock = new ();
#endif

    private ActivityListener? activityListener;
    private ActivityLifecycleLogEmitter? target;

    public DeferredActivityLifecycleLogEmitter(
        DeferredOperationRegistry operationRegistry,
        Func<ActivitySource, bool> shouldListenTo
    )
    {
        this.operationRegistry = operationRegistry;

        activityListener = new ActivityListener()
        {
            ActivityStarted = ActivityStarted,
            ActivityStopped = ActivityStopped,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ShouldListenTo = shouldListenTo,
        };
        ActivitySource.AddActivityListener(activityListener);
    }

    private void ActivityStarted(Activity activity)
    {
        // ReSharper disable once LocalVariableHidesMember
        if (this.target is { } target)
        {
            target.ActivityStarted(activity);
            return;
        }

        lock (@lock)
        {
            if ((target = this.target) is not null)
            {
                target.ActivityStarted(activity);
            }
            else
            {
                operationRegistry.Enqueue(new DeferredStartOperation(activity));
            }
        }
    }

    private void ActivityStopped(Activity activity)
    {
        // ReSharper disable once LocalVariableHidesMember
        if (this.target is { } target)
        {
            target.ActivityStopped(activity);
            return;
        }

        lock (@lock)
        {
            if ((target = this.target) is not null)
            {
                target.ActivityStopped(activity);
            }
            else
            {
                operationRegistry.Enqueue(new DeferredStopOperation(activity));
            }
        }
    }

    // ReSharper disable once ParameterHidesMember
    public void FlushTo(ActivityLifecycleLogEmitter target, bool throwOnFlushed = true)
    {
        if (this.target is not null)
        {
            if (throwOnFlushed)
                throw new InvalidOperationException("Already flushed");
            else
                return;
        }

        lock (@lock)
        {
            if (this.target is not null)
            {
                if (throwOnFlushed)
                    throw new InvalidOperationException("Already flushed");
                else
                    return;
            }

            this.target = target;

            UnregisterActivityListener();
        }

        operationRegistry.Flush(
            operation =>
            {
                if (operation is not DeferredOperation myOperation)
                    return false;

                myOperation.PrepareFlushTo(target);
                return true;
            }
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UnregisterActivityListener()
    {
        Interlocked.Exchange(ref activityListener, null)?.Dispose();
    }

    private abstract class DeferredOperation : IDeferredOperation
    {
        private ActivityLifecycleLogEmitter? target;

        protected ActivityLifecycleLogEmitter Target => target ?? throw new InvalidOperationException("Not flushable yet");

        public bool IsFlushable => target is not null;

        // ReSharper disable once ParameterHidesMember
        public void PrepareFlushTo(ActivityLifecycleLogEmitter target)
        {
            this.target = target;
        }

        public abstract void Flush();
    }

    private sealed class DeferredStartOperation : DeferredOperation
    {
        private readonly Activity activity;

        public DeferredStartOperation(Activity activity)
        {
            this.activity = activity;
        }

        public override void Flush() => Target.ActivityStarted(activity);
    }

    private sealed class DeferredStopOperation : DeferredOperation
    {
        private readonly Activity activity;

        public DeferredStopOperation(Activity activity)
        {
            this.activity = activity;
        }

        public override void Flush() => Target.ActivityStopped(activity);
    }
}
