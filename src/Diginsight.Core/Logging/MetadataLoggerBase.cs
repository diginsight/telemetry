using Microsoft.Extensions.Logging;

namespace Diginsight.Logging;

/// <summary>
/// Represents a base class for loggers that include metadata.
/// </summary>
public abstract class MetadataLoggerBase : ILogger
{
    private readonly ILogger decoratee;

    /// <summary>
    /// Gets the metadata associated with the logger.
    /// </summary>
    public abstract ILogMetadata Metadata { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataLoggerBase" /> class.
    /// </summary>
    /// <param name="decoratee">The underlying logger to decorate.</param>
    protected MetadataLoggerBase(ILogger decoratee)
    {
        this.decoratee = decoratee;
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var (newState, newFormatter) = LogMetadataCarrier.For(state, Metadata, formatter);
        decoratee.Log(logLevel, eventId, newState, exception, newFormatter);
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => decoratee.IsEnabled(logLevel);

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => decoratee.BeginScope(state);
}
