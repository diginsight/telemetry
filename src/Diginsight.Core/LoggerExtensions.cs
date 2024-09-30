using Microsoft.Extensions.Logging;

namespace Diginsight;

public static class LoggerExtensions
{
    public static ILogger WithMetadata(this ILogger logger, ILogMetadata metadata)
    {
        return new MetadataLogger(logger, metadata);
    }
}
