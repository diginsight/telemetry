using Diginsight.CAOptions;
using Diginsight.Strings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public sealed class DeferredLoggerFactory : IDeferredLoggerFactory
{
    private readonly TimeProvider timeProvider;
    private readonly object lockObj = new ();
    private readonly IDictionary<string, DeferredLogger> loggers = new Dictionary<string, DeferredLogger>(StringComparer.Ordinal);
    private readonly ConcurrentQueue<DeferredOperation> operations = new ();

    private ActivityListener? activityListener;
    private ILoggerFactory? target;

    public ISet<ActivitySource> ActivitySources { get; } = new HashSet<ActivitySource>();

    public DeferredLoggerFactory(
        TimeProvider? timeProvider = null,
        IAppendingContextFactory? appendingContextFactory = null,
        DiginsightActivitiesOptions? activitiesOptions = null,
        IActivityLoggingSampler? activityLoggingSampler = null
    )
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;

        ActivityLifecycleLogEmitter emitter = new (
            this,
            appendingContextFactory ?? new AppendingContextFactoryBuilder().WithLoggerFactory(this).Build(),
            new FixedClassAwareOptionsMonitor(activitiesOptions ?? new DiginsightActivitiesOptions()),
            activityLoggingSampler
        );
        activityListener = new DeferredActivityLifecycleLogEmitter(this, emitter).ToActivityListener(static _ => true);
        ActivitySource.AddActivityListener(activityListener);
    }

    private sealed class DeferredActivityLifecycleLogEmitter : IActivityListenerLogic
    {
        private readonly DeferredLoggerFactory owner;
        private readonly IActivityListenerLogic decoratee;

        public DeferredActivityLifecycleLogEmitter(DeferredLoggerFactory owner, IActivityListenerLogic decoratee)
        {
            this.owner = owner;
            this.decoratee = decoratee;
        }

        void IActivityListenerLogic.ActivityStarted(Activity activity)
        {
            if (!owner.ActivitySources.Contains(activity.Source) || owner.target is not null)
                return;

            lock (owner.lockObj)
            {
                if (owner.target is not null)
                    return;

                decoratee.ActivityStarted(activity);
            }
        }

        void IActivityListenerLogic.ActivityStopped(Activity activity)
        {
            if (!owner.ActivitySources.Contains(activity.Source) || owner.target is not null)
                return;

            lock (owner.lockObj)
            {
                if (owner.target is not null)
                    return;

                decoratee.ActivityStopped(activity);
            }
        }

        ActivitySamplingResult IActivityListenerLogic.Sample(ref ActivityCreationOptions<ActivityContext> creationOptions)
        {
            if (!owner.ActivitySources.Contains(creationOptions.Source) || owner.target is not null)
                return ActivitySamplingResult.None;

            lock (owner.lockObj)
            {
                if (owner.target is not null)
                    return ActivitySamplingResult.None;

                return decoratee.Sample(ref creationOptions);
            }
        }
    }

    private sealed class FixedClassAwareOptionsMonitor : IClassAwareOptionsMonitor<DiginsightActivitiesOptions>
    {
        private readonly DiginsightActivitiesOptions underlying;

        DiginsightActivitiesOptions IOptionsMonitor<DiginsightActivitiesOptions>.CurrentValue => underlying;

        public FixedClassAwareOptionsMonitor(DiginsightActivitiesOptions underlying)
        {
            this.underlying = underlying;
        }

        public DiginsightActivitiesOptions Get(string? name, Type? @class) => underlying;

        DiginsightActivitiesOptions IOptionsMonitor<DiginsightActivitiesOptions>.Get(string? name) => underlying;

        public IDisposable? OnChange(Action<DiginsightActivitiesOptions, string, Type> listener) => null;

        IDisposable? IOptionsMonitor<DiginsightActivitiesOptions>.OnChange(Action<DiginsightActivitiesOptions, string?> listener) => null;
    }

    public ILogger CreateLogger(string categoryName)
    {
        // ReSharper disable once LocalVariableHidesMember
        if (this.target is { } target)
        {
            return target.CreateLogger(categoryName);
        }

        lock (lockObj)
        {
            if ((target = this.target) is not null)
            {
                return target.CreateLogger(categoryName);
            }

            return loggers.TryGetValue(categoryName, out DeferredLogger? logger)
                ? logger
                : loggers[categoryName] = new DeferredLogger(this, categoryName);
        }
    }

    public void AddProvider(ILoggerProvider provider)
    {
        // ReSharper disable once LocalVariableHidesMember
        if (this.target is { } target)
        {
            target.AddProvider(provider);
            return;
        }

        lock (lockObj)
        {
            if ((target = this.target) is not null)
            {
                target.AddProvider(provider);
            }
            else
            {
                throw new NotSupportedException("Logger factory is deferred");
            }
        }
    }

    // ReSharper disable once ParameterHidesMember
    public void FlushTo(ILoggerFactory target, bool throwOnFlushed)
    {
        if (this.target is not null)
        {
            if (throwOnFlushed)
                throw new InvalidOperationException("Already flushed");
            else
                return;
        }

        lock (lockObj)
        {
            if (this.target is not null)
            {
                if (throwOnFlushed)
                    throw new InvalidOperationException("Already flushed");
                else
                    return;
            }

            this.target = target;
            loggers.Clear();
            Interlocked.Exchange(ref activityListener, null)?.Dispose();
        }

        while (operations.TryDequeue(out DeferredOperation? operation))
        {
            operation.FlushTo(target);
        }
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref activityListener, null)?.Dispose();
    }

    private sealed class DeferredLogger : ILogger
    {
        private readonly DeferredLoggerFactory owner;
        private readonly string category;

        public DeferredLogger(DeferredLoggerFactory owner, string category)
        {
            this.owner = owner;
            this.category = category;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (owner.target is { } target)
            {
                target.CreateLogger(category).Log(logLevel, eventId, state, exception, formatter);
                return;
            }

            lock (owner.lockObj)
            {
                if ((target = owner.target) is not null)
                {
                    target.CreateLogger(category).Log(logLevel, eventId, state, exception, formatter);
                }
                else
                {
                    owner.operations.Enqueue(new DeferredLogOperation<TState>(
                        category, GetTimestamp(), Activity.Current, logLevel, eventId, state, exception, formatter
                    ));
                }
            }
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            if (owner.target is { } target)
            {
                return target.CreateLogger(category).BeginScope(state);
            }

            lock (owner.lockObj)
            {
                if ((target = owner.target) is not null)
                {
                    return target.CreateLogger(category).BeginScope(state);
                }

                StrongBox<IDisposable?> scopeBox = new ();
                owner.operations.Enqueue(new DeferredBeginScopeOperation<TState>(category, state, scopeBox));
                return new CallbackDisposable(() => { owner.operations.Enqueue(new DeferredEndScopeOperation(scopeBox)); });
            }
        }

        private DateTimeOffset GetTimestamp() => owner.timeProvider.GetUtcNow();
    }

    private abstract class DeferredOperation
    {
        public abstract void FlushTo(ILoggerFactory target);
    }

    private sealed class DeferredLogOperation<TState> : DeferredOperation
    {
        private readonly string category;
        private readonly DateTimeOffset timestamp;
        private readonly Activity? activity;
        private readonly LogLevel logLevel;
        private readonly EventId eventId;
        private readonly TState state;
        private readonly Exception? exception;
        private readonly Func<TState, Exception?, string> formatter;

        public DeferredLogOperation(
            string category,
            DateTimeOffset timestamp,
            Activity? activity,
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            this.category = category;
            this.timestamp = timestamp;
            this.activity = activity;
            this.logLevel = logLevel;
            this.eventId = eventId;
            this.state = state;
            this.exception = exception;
            this.formatter = formatter;
        }

        public override void FlushTo(ILoggerFactory target)
        {
            target
                .CreateLogger(category)
                .Log(
                    logLevel,
                    eventId,
                    Deferred<TState>.For(state, timestamp, activity),
                    exception,
                    (s, e) => formatter(s.State, e)
                );
        }
    }

    public interface IDeferred
    {
        object? State { get; }
        DateTimeOffset Timestamp { get; }
        Activity? Activity { get; }
    }

    public interface IDeferred<out TState> : IDeferred
    {
        new TState State { get; }

#if NET || NETSTANDARD2_1_OR_GREATER
        object? IDeferred.State => State;
#endif
    }

    private class Deferred<TState> : IDeferred<TState>
    {
        public TState State { get; }
        public DateTimeOffset Timestamp { get; }
        public Activity? Activity { get; }

#if !(NET || NETSTANDARD2_1_OR_GREATER)
        object? IDeferred.State => State;
#endif

        public Deferred(TState state, DateTimeOffset timestamp, Activity? activity)
        {
            State = state;
            Timestamp = timestamp;
            Activity = activity;
        }

        public static IDeferred<TState> For(TState state, DateTimeOffset timestamp, Activity? activity)
        {
            return state is Tags kvps
                ? new TagsDeferred<TState>(state, kvps, timestamp, activity)
                : new Deferred<TState>(state, timestamp, activity);
        }
    }

    private sealed class TagsDeferred<TState> : Deferred<TState>, Tags
    {
        private readonly Tags kvps;

        public TagsDeferred(TState state, Tags kvps, DateTimeOffset timestamp, Activity? activity)
            : base(state, timestamp, activity)
        {
            this.kvps = kvps;
        }

        public IEnumerator<Tag> GetEnumerator() => kvps.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class DeferredBeginScopeOperation<TState> : DeferredOperation
        where TState : notnull
    {
        private readonly string category;
        private readonly TState state;
        private readonly StrongBox<IDisposable?> scopeBox;

        public DeferredBeginScopeOperation(
            string category,
            TState state,
            StrongBox<IDisposable?> scopeBox
        )
        {
            this.category = category;
            this.state = state;
            this.scopeBox = scopeBox;
        }

        public override void FlushTo(ILoggerFactory target)
        {
            scopeBox.Value = target.CreateLogger(category).BeginScope(state);
        }
    }

    private sealed class DeferredEndScopeOperation : DeferredOperation
    {
        private readonly StrongBox<IDisposable?> scopeBox;

        public DeferredEndScopeOperation(StrongBox<IDisposable?> scopeBox)
        {
            this.scopeBox = scopeBox;
        }

        public override void FlushTo(ILoggerFactory target)
        {
            scopeBox.Value?.Dispose();
        }
    }
}
