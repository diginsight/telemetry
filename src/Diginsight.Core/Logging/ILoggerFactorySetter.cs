using Microsoft.Extensions.Logging;

namespace Diginsight.Logging;

/// <summary>
///     Interface for setting the current <see cref="ILoggerFactory"/>.
/// </summary>
public interface ILoggerFactorySetter : ILoggerFactory
{
    /// <summary>
    ///     Gets the collection of <see cref="ILoggerProvider"/>.
    /// </summary>
    IEnumerable<ILoggerProvider> LoggerProviders { get; }

    /// <summary>
    ///     Sets the current logger factory.
    /// </summary>
    /// <param name="loggerFactory">The logger factory to set.</param>
    /// <returns>An <see cref="IDisposable"/> that can be used to revert the logger factory.</returns>
    IDisposable WithLoggerFactory(ILoggerFactory loggerFactory);
}
