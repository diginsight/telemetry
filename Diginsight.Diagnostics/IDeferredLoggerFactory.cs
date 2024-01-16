using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics;

public interface IDeferredLoggerFactory : ILoggerFactory
{
    void FlushTo(ILoggerFactory target);
}
