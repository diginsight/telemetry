using Diginsight.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public sealed class DeferredLoggerFactory : ILoggerFactory
{
    private readonly DeferredOperationRegistry operationRegistry;
    private readonly TimeProvider timeProvider;
    private readonly Func<ILoggerFactory>? makeEmergencyTarget;
#if NET9_0_OR_GREATER
    private readonly Lock @lock = new ();
#else
    private readonly object @lock = new ();
#endif
    private readonly IDictionary<string, DeferredLogger> loggers = new Dictionary<string, DeferredLogger>(StringComparer.Ordinal);

    private ILoggerFactory? target;

    public DeferredLoggerFactory(
        DeferredOperationRegistry operationRegistry,
        TimeProvider? timeProvider = null,
        Func<ILoggerFactory>? makeEmergencyTarget = null
    )
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.operationRegistry = operationRegistry;
        this.makeEmergencyTarget = makeEmergencyTarget;
    }

    ILogger ILoggerFactory.CreateLogger(string categoryName)
    {
        // ReSharper disable once LocalVariableHidesMember
        if (this.target is { } target)
        {
            return target.CreateLogger(categoryName);
        }

        lock (@lock)
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

    void ILoggerFactory.AddProvider(ILoggerProvider provider)
    {
        // ReSharper disable once LocalVariableHidesMember
        if (this.target is { } target)
        {
            target.AddProvider(provider);
            return;
        }

        lock (@lock)
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

    public void FlushTo(
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        ILoggerFactory target,
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

    void IDisposable.Dispose()
    {
        if (makeEmergencyTarget is null)
            return;

        if (target is not null)
            return;

        lock (@lock)
        {
            if (target is not null)
                return;

            ILoggerFactory emergencyTarget = makeEmergencyTarget();
            operationRegistry.AddDisposable(emergencyTarget);

            SetTarget(emergencyTarget);
        }

        FlushOperations();
    }

    private void SetTarget(
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        ILoggerFactory target
    )
    {
        this.target = target;

        loggers.Clear();
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

            lock (owner.@lock)
            {
                if ((target = owner.target) is not null)
                {
                    target.CreateLogger(category).Log(logLevel, eventId, state, exception, formatter);
                }
                else
                {
                    owner.operationRegistry.Enqueue(
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

            lock (owner.@lock)
            {
                if ((target = owner.target) is not null)
                {
                    return target.CreateLogger(category).BeginScope(state);
                }

                StrongBox<IDisposable?> scopeBox = new ();
                DeferredOperationRegistry operationRegistry = owner.operationRegistry;

                operationRegistry.Enqueue(new DeferredBeginScopeOperation<TState>(category, state, scopeBox));
                return new CallbackDisposable(() => { operationRegistry.Enqueue(new DeferredEndScopeOperation(scopeBox)); });
            }
        }

        private DateTimeOffset GetTimestamp() => owner.timeProvider.GetUtcNow();
    }

    private abstract class DeferredOperation : IDeferredOperation
    {
        private ILoggerFactory? target;

        protected ILoggerFactory Target => target ?? throw new InvalidOperationException("Not flushable yet");

        bool IDeferredOperation.IsFlushable => target is not null;

        public void PrepareFlushTo(
            [SuppressMessage("ReSharper", "ParameterHidesMember")]
            ILoggerFactory target
        )
        {
            this.target = target;
        }

        protected abstract void Flush();

        void IDeferredOperation.Flush() => Flush();

        void IDeferredOperation.Discard() => PrepareFlushTo(NullLoggerFactory.Instance);
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

        protected override void Flush()
        {
            Target.CreateLogger(category).WithMetadata(metadata).Log(logLevel, eventId, state, exception, formatter);
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

        protected override void Flush()
        {
            scopeBox.Value = Target.CreateLogger(category).BeginScope(state);
        }
    }

    private sealed class DeferredEndScopeOperation : DeferredOperation
    {
        private readonly StrongBox<IDisposable?> scopeBox;

        public DeferredEndScopeOperation(StrongBox<IDisposable?> scopeBox)
        {
            this.scopeBox = scopeBox;
        }

        protected override void Flush()
        {
            scopeBox.Value?.Dispose();
        }
    }
}
