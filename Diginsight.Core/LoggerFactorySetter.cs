using Microsoft.Extensions.Logging;

namespace Diginsight;

internal sealed class LoggerFactorySetter : ILoggerFactorySetter
{
    private readonly ILoggerFactory decoratee;
    private readonly ICollection<ILoggerProvider> loggerProviders;
    private readonly AsyncLocal<ILoggerFactory?> asyncLocal = new ();

    public IEnumerable<ILoggerProvider> LoggerProviders => loggerProviders;

    private bool IsRoot => asyncLocal.Value is null;
    private ILoggerFactory Underying => asyncLocal.Value ?? decoratee;

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
        Underying.AddProvider(provider);
        if (IsRoot)
        {
            loggerProviders.Add(provider);
        }
    }

    void IDisposable.Dispose()
    {
        if (IsRoot)
        {
            decoratee.Dispose();
        }
    }

    private sealed class RedirectorLogger : ILogger
    {
        private readonly LoggerFactorySetter setter;
        private readonly string categoryName;
        private readonly object @lock = new ();

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
                lock (@lock)
                {
                    ILogger logger;

                    if (current?.Factory != setter.Underying)
                    {
                        ILoggerFactory factory = setter.Underying;
                        logger = factory.CreateLogger(categoryName);
                        current = (logger, factory);
                    }
                    else
                    {
                        logger = current.Value.Logger;
                    }

                    return logger;
                }
            }
        }

#if NET7_0_OR_GREATER
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
#else
        public IDisposable BeginScope<TState>(TState state)
#endif
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
