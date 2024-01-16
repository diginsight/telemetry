using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics;

public sealed class DeferredLoggerFactory : IDeferredLoggerFactory
{
    private readonly TimeProvider timeProvider;
    private readonly object lockObj = new ();

    private readonly IDictionary<string, DeferredLogger> loggers = new Dictionary<string, DeferredLogger>(StringComparer.Ordinal);
    private readonly ConcurrentQueue<DeferredOperation> operations = new ();

    private ILoggerFactory? target;

    public DeferredLoggerFactory(
        TimeProvider? timeProvider = null
    )
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
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
                    owner.operations.Enqueue(new DeferredBeginScopeOperation<TState>(category, GetTimestamp(), state, scopeBox));
                    return new CallbackDisposable(() => { owner.operations.Enqueue(new DeferredEndScopeOperation(scopeBox)); });
                }
            }
        }

        private DateTime GetTimestamp() => owner.timeProvider.GetUtcNow().UtcDateTime;
    }

    private abstract class DeferredOperation
    {
        public abstract void FlushTo(ILoggerFactory target);
    }

    private sealed class DeferredLogOperation<TState> : DeferredOperation
    {
        private readonly string category;
        private readonly DateTime timestamp;
        private readonly LogLevel logLevel;
        private readonly EventId eventId;
        private readonly TState state;
        private readonly Exception? exception;
        private readonly Func<TState, Exception?, string> formatter;

        public DeferredLogOperation(
            string category,
            DateTime timestamp,
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
            target.CreateLogger(category).Log(logLevel, eventId, state, exception, formatter);
        }
    }

    private sealed class DeferredBeginScopeOperation<TState> : DeferredOperation
        where TState : notnull
    {
        private readonly string category;
        private readonly DateTime timestamp;
        private readonly TState state;
        private readonly StrongBox<IDisposable?> scopeBox;

        public DeferredBeginScopeOperation(
            string category,
            DateTime timestamp,
            TState state,
            StrongBox<IDisposable?> scopeBox
        )
        {
            this.category = category;
            this.timestamp = timestamp;
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
