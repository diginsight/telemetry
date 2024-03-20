using Diginsight.CAOptions;
using Diginsight.Strings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
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

    private ILoggerFactory? target;

    public ActivitySource ActivitySource { get; } = new ($"{typeof(DeferredLoggerFactory).FullName!}_{Guid.NewGuid():N}");

    public DeferredLoggerFactory(
        TimeProvider? timeProvider = null,
        IAppendingContextFactory? appendingContextFactory = null,
        DiginsightActivitiesOptions? activitiesOptions = null,
        IActivityProcessingSampler? activityProcessingSampler = null
    )
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;

        BaseProcessor<Activity> processor = new DiginsightLogProcessor(
            this,
            appendingContextFactory ?? AppendingContextFactoryBuilder.DefaultFactory,
            new FixedClassAwareOptionsMonitor(activitiesOptions ?? new DiginsightActivitiesOptions()),
            activityProcessingSampler
        );

        ActivityListener listener = new ActivityListener()
        {
            ActivityStarted = processor.OnStart,
            ActivityStopped = processor.OnEnd,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ShouldListenTo = s => s == ActivitySource,
        };

        ActivitySource.AddActivityListener(listener);
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
        lock (lockObj)
        {
            if (target is not null)
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
        lock (lockObj)
        {
            if (target is null)
            {
                throw new NotSupportedException("Logger factory is deferred");
            }

            target.AddProvider(provider);
        }
    }

    // ReSharper disable once ParameterHidesMember
    public void FlushTo(ILoggerFactory target)
    {
        lock (lockObj)
        {
            if (this.target is not null)
            {
                throw new InvalidOperationException("Already flushed");
            }

            this.target = target;
            loggers.Clear();
        }

        while (operations.TryDequeue(out DeferredOperation? operation))
        {
            operation.FlushTo(target);
        }
    }

    public void Dispose() { }

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
            lock (owner.lockObj)
            {
                if (owner.target is { } target)
                {
                    target.CreateLogger(category).Log(logLevel, eventId, state, exception, formatter);
                }
                else
                {
                    owner.operations.Enqueue(new DeferredLogOperation<TState>(category, GetTimestamp(), logLevel, eventId, state, exception, formatter));
                }
            }
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            lock (owner.lockObj)
            {
                if (owner.target is { } target)
                {
                    return target.CreateLogger(category).BeginScope(state);
                }
                else
                {
                    StrongBox<IDisposable?> scopeBox = new ();
                    owner.operations.Enqueue(new DeferredBeginScopeOperation<TState>(category, state, scopeBox));
                    return new CallbackDisposable(() => { owner.operations.Enqueue(new DeferredEndScopeOperation(scopeBox)); });
                }
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
        private readonly LogLevel logLevel;
        private readonly EventId eventId;
        private readonly TState state;
        private readonly Exception? exception;
        private readonly Func<TState, Exception?, string> formatter;

        public DeferredLogOperation(
            string category,
            DateTimeOffset timestamp,
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            this.category = category;
            this.timestamp = timestamp;
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
                    Timestamped<TState>.For(state, timestamp),
                    exception,
                    (s, e) => formatter(s.State, e)
                );
        }
    }

    public interface ITimestamped
    {
        object? State { get; }
        DateTimeOffset Timestamp { get; }
    }

    public interface ITimestamped<out TState> : ITimestamped
    {
        new TState State { get; }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        object? ITimestamped.State => State;
#endif
    }

    private class Timestamped<TState> : ITimestamped<TState>
    {
        public TState State { get; }
        public DateTimeOffset Timestamp { get; }

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
        object? ITimestamped.State => State;
#endif

        public Timestamped(TState state, DateTimeOffset timestamp)
        {
            State = state;
            Timestamp = timestamp;
        }

        public static ITimestamped<TState> For(TState state, DateTimeOffset timestamp)
        {
            return state is Tags kvps ? new TagsTimestamped<TState>(state, kvps, timestamp) : new Timestamped<TState>(state, timestamp);
        }
    }

    private sealed class TagsTimestamped<TState> : Timestamped<TState>, Tags
    {
        private readonly Tags kvps;

        public TagsTimestamped(TState state, Tags kvps, DateTimeOffset timestamp)
            : base(state, timestamp)
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
