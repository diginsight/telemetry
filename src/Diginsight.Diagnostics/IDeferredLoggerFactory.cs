using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Diginsight.Diagnostics;

public interface IDeferredLoggerFactory : ILoggerFactory
{
    ISet<ActivitySource> ActivitySources { get; }

    void FlushTo(ILoggerFactory target, bool throwOnFlushed = true);
}
