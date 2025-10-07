using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics;

public static class LoggerFactoryStaticAccessor
{
    public static ILoggerFactory? LoggerFactory { get; set; }
}
