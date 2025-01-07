using Diginsight.Logging;
using Diginsight.Options;
using Diginsight.Stringify;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if !NET9_0_OR_GREATER
using Lock = object;
#endif

namespace Diginsight.Diagnostics;

public sealed class DeferredLoggerFactory : IDeferredLoggerFactory
{
    private readonly TimeProvider timeProvider;
    private readonly Func<ILoggerFactory>? makeEmergencyLoggerFactory;
    private readonly Lock lockObj = new ();
    private readonly IDictionary<string, DeferredLogger> loggers = new Dictionary<string, DeferredLogger>(StringComparer.Ordinal);
    private readonly ConcurrentQueue<DeferredOperation> operations = new ();

    private ActivityListener? activityListener;
    private ILoggerFactory? target;

    public Func<ActivitySource, bool>? ActivitySourceFilter { get; set; }

    public DeferredLoggerFactory(
        TimeProvider? timeProvider = null,
        IStringifyContextFactory? stringifyContextFactory = null,
        DiginsightActivitiesOptions? activitiesOptions = null,
        IActivityLoggingSampler? activityLoggingSampler = null,
        Func<ILoggerFactory>? makeEmergencyLoggerFactory = null
    )
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.makeEmergencyLoggerFactory = makeEmergencyLoggerFactory;

        ActivityLifecycleLogEmitter emitter = new (
            this,
            stringifyContextFactory ?? new StringifyContextFactoryBuilder().WithLoggerFactory(this).Build(),
            new FixedClassAwareOptionsMonitor(activitiesOptions ?? new DiginsightActivitiesOptions()),
            activityLoggingSampler
        );
        activityListener = new DeferredActivityLifecycleLogEmitter(this, emitter).ToActivityListener(static _ => true);
        ActivitySource.AddActivityListener(activityListener);
    }

    public static ILoggerFactory MakeDefaultEmergencyLoggerFactory(
        Action<LoggerFilterOptions>? configureFilterOptions = null,
        Action<DiginsightConsoleFormatterOptions>? configureFormatterOptions = null
    )
    {
        IServiceCollection services = new ServiceCollection()
            .AddLogging(lb => { lb.AddDiginsightConsole(configureFormatterOptions); });

        if (configureFilterOptions is not null)
        {
            services.Configure(configureFilterOptions);
        }

        return services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsIncluded(ActivitySource activitySource) => owner.ActivitySourceFilter?.Invoke(activitySource) ?? true;

        void IActivityListenerLogic.ActivityStarted(Activity activity)
        {
            if (!IsIncluded(activity.Source) || owner.target is not null)
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
            if (!IsIncluded(activity.Source) || owner.target is not null)
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
            if (!IsIncluded(creationOptions.Source) || owner.target is not null)
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
            UnregisterActivityListener();
        }

        while (operations.TryDequeue(out DeferredOperation? operation))
        {
            operation.FlushTo(target);
        }
    }

    public void Dispose()
    {
        try
        {
            if (makeEmergencyLoggerFactory is null)
                return;

            using ILoggerFactory emergencyLoggerFactory = makeEmergencyLoggerFactory();
            FlushTo(emergencyLoggerFactory, false);
        }
        finally
        {
            UnregisterActivityListener();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UnregisterActivityListener()
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
                    owner.operations.Enqueue(
                        new DeferredLogOperation<TState>(
                            category, GetTimestamp(), Activity.Current, logLevel, eventId, state, exception, formatter
                        )
                    );
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
        private readonly LogLevel logLevel;
        private readonly EventId eventId;
        private readonly TState state;
        private readonly Exception? exception;
        private readonly Func<TState, Exception?, string> formatter;
        private readonly ILogMetadata metadata;

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
            this.logLevel = logLevel;
            this.eventId = eventId;
            this.state = state;
            this.exception = exception;
            this.formatter = formatter;
            metadata = new LogMetadata(timestamp, activity);
        }

        public override void FlushTo(ILoggerFactory target)
        {
            target.CreateLogger(category).WithMetadata(metadata).Log(logLevel, eventId, state, exception, formatter);
        }
    }

    public interface ILogMetadata : Logging.ILogMetadata
    {
        DateTimeOffset Timestamp { get; }
        Activity? Activity { get; }
    }

    private sealed class LogMetadata : ILogMetadata
    {
        public DateTimeOffset Timestamp { get; }
        public Activity? Activity { get; }

        public LogMetadata(DateTimeOffset timestamp, Activity? activity)
        {
            Timestamp = timestamp;
            Activity = activity;
        }
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
