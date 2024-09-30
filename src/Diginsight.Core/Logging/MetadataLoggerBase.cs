using Microsoft.Extensions.Logging;

namespace Diginsight.Logging;

public abstract class MetadataLoggerBase : ILogger
{
    private readonly ILogger decoratee;

    public abstract ILogMetadata Metadata { get; }

    protected MetadataLoggerBase(ILogger decoratee)
    {
        this.decoratee = decoratee;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var (newState, newFormatter) = LogMetadataCarrier.For(state, Metadata, formatter);
        decoratee.Log(logLevel, eventId, newState, exception, newFormatter);
    }

    public bool IsEnabled(LogLevel logLevel) => decoratee.IsEnabled(logLevel);

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => decoratee.BeginScope(state);
}
