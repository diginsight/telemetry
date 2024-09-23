using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Diginsight.Diagnostics;

public interface IDeferredLoggerFactory : ILoggerFactory
{
    Func<ActivitySource, bool>? ActivitySourceFilter { get; set; }

    void FlushTo(ILoggerFactory target, bool throwOnFlushed = true);
}
