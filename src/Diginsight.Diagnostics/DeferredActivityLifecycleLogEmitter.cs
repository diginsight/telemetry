using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

[SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
public sealed class DeferredActivityLifecycleLogEmitter : IDisposable
{
    private readonly DeferredOperationRegistry operationRegistry;
    private readonly TimeProvider timeProvider;
    private readonly Func<ActivityLifecycleLogEmitter>? makeEmergencyTarget;
#if NET9_0_OR_GREATER
    private readonly Lock @lock = new ();
#else
    private readonly object @lock = new ();
#endif

    private ActivityListener? activityListener;
    private ActivityLifecycleLogEmitter? target;

    public DeferredActivityLifecycleLogEmitter(
        DeferredOperationRegistry operationRegistry,
        Func<ActivitySource, bool> shouldListenTo,
        TimeProvider? timeProvider = null,
        Func<ActivityLifecycleLogEmitter>? makeEmergencyTarget = null
    )
    {
        this.operationRegistry = operationRegistry;
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.makeEmergencyTarget = makeEmergencyTarget;

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
                activity.SetCustomProperty(ActivityCustomPropertyNames.EmitStartTimestamp, timeProvider.GetUtcNow());
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
                activity.SetCustomProperty(ActivityCustomPropertyNames.EmitStopTimestamp, timeProvider.GetUtcNow());
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

            SetTarget(target);
        }

        FlushOperations();
    }

    void IDisposable.Dispose()
    {
        UnregisterActivityListener();

        if (makeEmergencyTarget is null)
            return;

        if (target is not null)
            return;

        lock (@lock)
        {
            if (target is not null)
                return;

            ActivityLifecycleLogEmitter emergencyTarget = makeEmergencyTarget();
            SetTarget(emergencyTarget);
        }

        FlushOperations();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UnregisterActivityListener()
    {
        Interlocked.Exchange(ref activityListener, null)?.Dispose();
    }

    // ReSharper disable once ParameterHidesMember
    private void SetTarget(ActivityLifecycleLogEmitter target)
    {
        this.target = target;

        UnregisterActivityListener();
    }

    private void FlushOperations()
    {
        operationRegistry.Flush(
            operation =>
            {
                if (operation is not DeferredOperation myOperation)
                    return false;

                myOperation.PrepareFlushTo(target!);
                return true;
            }
        );
    }

    private abstract class DeferredOperation : IDeferredOperation
    {
        private ActivityLifecycleLogEmitter? target;

        protected ActivityLifecycleLogEmitter Target => target ?? throw new InvalidOperationException("Not flushable yet");

        bool IDeferredOperation.IsFlushable => target is not null;

        // ReSharper disable once ParameterHidesMember
        public void PrepareFlushTo(ActivityLifecycleLogEmitter target)
        {
            this.target = target;
        }

        protected abstract void Flush();

        void IDeferredOperation.Flush() => Flush();

        void IDeferredOperation.Discard() => PrepareFlushTo(ActivityLifecycleLogEmitter.Noop);
    }

    private sealed class DeferredStartOperation : DeferredOperation
    {
        private readonly Activity activity;

        public DeferredStartOperation(Activity activity)
        {
            this.activity = activity;
        }

        protected override void Flush() => Target.ActivityStarted(activity);
    }

    private sealed class DeferredStopOperation : DeferredOperation
    {
        private readonly Activity activity;

        public DeferredStopOperation(Activity activity)
        {
            this.activity = activity;
        }

        protected override void Flush() => Target.ActivityStopped(activity);
    }
}
