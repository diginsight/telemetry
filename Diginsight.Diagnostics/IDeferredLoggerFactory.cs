using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Diginsight.Diagnostics;

public interface IDeferredLoggerFactory : ILoggerFactory
{
    ActivitySource ActivitySource { get; }

    void FlushTo(ILoggerFactory target);
}
