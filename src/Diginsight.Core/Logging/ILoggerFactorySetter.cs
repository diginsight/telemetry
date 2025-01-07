using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Diginsight.Logging;

/// <summary>
/// Interface for setting the current <see cref="ILoggerFactory" />.
/// </summary>
/// <remarks>
/// The aim of this interface is to provide different per-async-scope instances of <see cref="ILoggerFactory" />,
/// bypassing the limitation of <see cref="ILoggerFactory" /> being registered as a <see cref="ServiceLifetime.Singleton" /> service.
/// </remarks>
public interface ILoggerFactorySetter : ILoggerFactory
{
    /// <summary>
    /// Gets the collection of registered logger providers.
    /// </summary>
    IEnumerable<ILoggerProvider> LoggerProviders { get; }

    /// <summary>
    /// Gets the current logger factory.
    /// </summary>
    ILoggerFactory Current { get; }

    /// <summary>
    /// Sets the current logger factory.
    /// </summary>
    /// <param name="loggerFactory">The logger factory to set.</param>
    /// <returns>An <see cref="IDisposable" /> that can be used to revert the logger factory.</returns>
    IDisposable WithLoggerFactory(ILoggerFactory loggerFactory);
}
