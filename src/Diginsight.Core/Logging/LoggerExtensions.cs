using Microsoft.Extensions.Logging;

namespace Diginsight.Logging;

/// <summary>
///     Provides extension methods for the <see cref="ILogger" /> interface.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    ///     Adds metadata to the logger.
    /// </summary>
    /// <param name="logger">The logger to which metadata will be added.</param>
    /// <param name="metadata">The metadata to add to the logger.</param>
    /// <returns>A new <see cref="ILogger" /> instance with the specified metadata.</returns>
    public static ILogger WithMetadata(this ILogger logger, ILogMetadata metadata)
    {
        return new MetadataLogger(logger, metadata);
    }
}
