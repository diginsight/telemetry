using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents an activity lifecycle log emitter that defers activity lifecycle logging until a target emitter is available.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
public sealed class DeferredActivityLifecycleLogEmitter : IDisposable
{
    private readonly DeferredOperationRegistry operationRegistry;
    private readonly TimeProvider timeProvider;
    private readonly Func<ActivityLifecycleLogEmitter>? makeEmergencyTarget;

    private readonly
#if NET9_0_OR_GREATER
        Lock
#else
        object
#endif
        @lock = new ();

    private ActivityListener? activityListener;
    private ActivityLifecycleLogEmitter? target;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeferredActivityLifecycleLogEmitter" /> class.
    /// </summary>
    /// <param name="operationRegistry">The deferred operation registry.</param>
    /// <param name="shouldListenTo">The predicate used to determine whether an activity source should be listened to.</param>
    /// <param name="timeProvider">The time provider used to timestamp deferred operations.</param>
    /// <param name="makeEmergencyTarget">The factory used to create an emergency target emitter.</param>
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
            Sample = static (ref _) => ActivitySamplingResult.AllData,
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

    /// <summary>
    /// Flushes deferred activity lifecycle operations to the specified target emitter.
    /// </summary>
    /// <param name="target">The target activity lifecycle log emitter.</param>
    /// <param name="throwOnFlushed">A value indicating whether to throw when this instance has already been flushed.</param>
    /// <exception cref="InvalidOperationException">Thrown when this instance has already been flushed and <paramref name="throwOnFlushed" /> is <c>true</c>.</exception>
    public void FlushTo(
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        ActivityLifecycleLogEmitter target,
        bool throwOnFlushed = true
    )
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

    /// <inheritdoc />
    public void Dispose()
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

    private void SetTarget(
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        ActivityLifecycleLogEmitter target
    )
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

        public void PrepareFlushTo(
            [SuppressMessage("ReSharper", "ParameterHidesMember")]
            ActivityLifecycleLogEmitter target
        )
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
