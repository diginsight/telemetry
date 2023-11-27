using Microsoft.Extensions.Logging;

namespace Diginsight;

public interface ILoggerFactorySetter : ILoggerFactory
{
    IEnumerable<ILoggerProvider> LoggerProviders { get; }

    IDisposable WithLoggerFactory(ILoggerFactory loggerFactory);
}
