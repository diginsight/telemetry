#if NET7_0_OR_GREATER
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
#endif

namespace Diginsight.Diagnostics;

public static partial class LoggerExtensions
{
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
