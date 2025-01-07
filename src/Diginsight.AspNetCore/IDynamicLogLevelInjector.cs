using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Diginsight.AspNetCore;

/// <summary>
/// Allows per-HTTP-request customization of log levels.
/// </summary>
public interface IDynamicLogLevelInjector
{
    /// <summary>
    /// Creates, if necessary, a logger factory that emits logs according to the log levels specified in the HTTP context.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="loggerProviders">The current logger providers.</param>
    /// <returns>A new logger factory, or <c>null</c> if the HTTP context contains no log level specification.</returns>
    ILoggerFactory? TryCreateLoggerFactory(HttpContext context, IEnumerable<ILoggerProvider> loggerProviders);
}
