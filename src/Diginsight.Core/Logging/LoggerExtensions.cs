using Microsoft.Extensions.Logging;
#if NET7_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace Diginsight.Logging;

/// <summary>
/// Provides extension methods for the <see cref="ILogger" /> interface.
/// </summary>
public static partial class LoggerExtensions
{
    /// <summary>
    /// Adds metadata to the logger.
    /// </summary>
    /// <param name="logger">The logger to which metadata will be added.</param>
    /// <param name="metadata">The metadata to add to the logger.</param>
    /// <returns>A new <see cref="ILogger" /> instance with the specified metadata.</returns>
    public static ILogger WithMetadata(this ILogger logger, ILogMetadata metadata)
    {
        return new MetadataLogger(logger, metadata);
    }

#if NET7_0_OR_GREATER
    public static void Log(
        // ReSharper disable EntityNameCapturedOnly.Global
        this ILogger logger,
        LogLevel logLevel,
        // ReSharper restore EntityNameCapturedOnly.Global
        EventId eventId,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger), nameof(logLevel))]
        in LogInterpolatedStringHandler message
    )
    {
        message.LogIfEnabled(eventId, exception);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Log(
        this ILogger logger,
        LogLevel logLevel,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger), nameof(logLevel))]
        in LogInterpolatedStringHandler message
    )
    {
        Log(logger, logLevel, default, exception, in message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Log(
        this ILogger logger,
        LogLevel logLevel,
        [InterpolatedStringHandlerArgument(nameof(logger), nameof(logLevel))]
        in LogInterpolatedStringHandler message
    )
    {
        Log(logger, logLevel, null, in message);
    }
#endif
}
