using Microsoft.Extensions.Logging;
#if !NET9_0_OR_GREATER
using Lock = object;
#endif

namespace Diginsight.Logging;

internal sealed class LoggerFactorySetter : ILoggerFactorySetter
{
    private readonly ILoggerFactory decoratee;
    private readonly ICollection<ILoggerProvider> loggerProviders;
    private readonly AsyncLocal<ILoggerFactory?> asyncLocal = new ();

    public IEnumerable<ILoggerProvider> LoggerProviders => loggerProviders;

    public ILoggerFactory Current => Underlying.Factory;

    private (ILoggerFactory Factory, bool IsRoot) Underlying => asyncLocal.Value is { } factory ? (factory, false) : (decoratee, true);

    public LoggerFactorySetter(
        ILoggerFactory decoratee,
        IEnumerable<ILoggerProvider> loggerProviders
    )
    {
        this.decoratee = decoratee;
        this.loggerProviders = new List<ILoggerProvider>(loggerProviders);
    }

    public IDisposable WithLoggerFactory(ILoggerFactory loggerFactory)
    {
        ILoggerFactory? previous = asyncLocal.Value;
        asyncLocal.Value = loggerFactory;
        return new CallbackDisposable(() => { asyncLocal.Value = previous; });
    }

    ILogger ILoggerFactory.CreateLogger(string categoryName) => new RedirectorLogger(this, categoryName);

    void ILoggerFactory.AddProvider(ILoggerProvider provider)
    {
        (ILoggerFactory factory, bool isRoot) = Underlying;
        factory.AddProvider(provider);
        if (isRoot)
        {
            loggerProviders.Add(provider);
        }
    }

    void IDisposable.Dispose()
    {
        if (Underlying.IsRoot)
        {
            decoratee.Dispose();
        }
    }

    private sealed class RedirectorLogger : ILogger
    {
        private readonly LoggerFactorySetter setter;
        private readonly string categoryName;
        private readonly Lock @lock = new ();

        private (ILogger Logger, ILoggerFactory Factory)? current;

        public RedirectorLogger(
            LoggerFactorySetter setter,
            string categoryName
        )
        {
            this.setter = setter;
            this.categoryName = categoryName;

            current = null;
        }

        private ILogger ActualLogger
        {
            get
            {
                ILogger logger;
                ILoggerFactory factory = setter.Underlying.Factory;

                lock (@lock)
                {
                    if (current?.Factory != factory)
                    {
                        logger = factory.CreateLogger(categoryName);
                        current = (logger, factory);
                    }
                    else
                    {
                        logger = current.Value.Logger;
                    }
                }

                return logger;
            }
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return ActualLogger.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return ActualLogger.IsEnabled(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ActualLogger.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
