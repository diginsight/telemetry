using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics;

/// <summary>
/// Provides static access to an application logger factory.
/// </summary>
public static class LoggerFactoryStaticAccessor
{
    /// <summary>
    /// Gets the application logger factory.
    /// </summary>
    public static ILoggerFactory? LoggerFactory { get; set; }
}
