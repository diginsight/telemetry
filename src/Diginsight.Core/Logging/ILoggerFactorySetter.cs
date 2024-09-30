using Microsoft.Extensions.Logging;

namespace Diginsight.Logging;

public interface ILoggerFactorySetter : ILoggerFactory
{
    IEnumerable<ILoggerProvider> LoggerProviders { get; }

    IDisposable WithLoggerFactory(ILoggerFactory loggerFactory);
}
