using Microsoft.Extensions.Logging;

namespace Diginsight.Logging;

internal sealed class LoggerFactorySetter : ILoggerFactorySetter
{
    private readonly ILoggerFactory decoratee;
    private readonly ICollection<ILoggerProvider> loggerProviders;
    private readonly AsyncLocal<ILoggerFactory?> asyncLocal = new ();

    public IEnumerable<ILoggerProvider> LoggerProviders => loggerProviders;

    public ILoggerFactory Current => asyncLocal.Value ?? decoratee;

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
        if (asyncLocal.Value is { } factory)
        {
            factory.AddProvider(provider);
        }
        else
        {
            decoratee.AddProvider(provider);
            loggerProviders.Add(provider);
        }
    }

    void IDisposable.Dispose()
    {
        if (asyncLocal.Value is null)
        {
            decoratee.Dispose();
        }
    }

    private sealed class RedirectorLogger : ILogger
    {
        private readonly LoggerFactorySetter setter;
        private readonly string categoryName;
#if NET9_0_OR_GREATER
        private readonly Lock @lock = new ();
#else
        private readonly object @lock = new ();
#endif

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
                ILoggerFactory factory = setter.Current;

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
