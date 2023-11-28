using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Diginsight.AspNetCore;

public interface IDynamicLogLevelInjector
{
    ILoggerFactory? TryCreateLoggerFactory(HttpContext context, IEnumerable<ILoggerProvider> loggerProviders);
}
