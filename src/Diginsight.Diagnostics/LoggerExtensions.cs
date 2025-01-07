#if NET7_0_OR_GREATER
using Diginsight.Logging;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
#endif

namespace Diginsight.Diagnostics;

public static partial class LoggerExtensions
{
#if NET7_0_OR_GREATER
    [Obsolete("Moved to `Diginsight.Logging` namespace")]
#if NET9_0_OR_GREATER
    [OverloadResolutionPriority(-1)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Log(
        // ReSharper disable EntityNameCapturedOnly.Global
        this ILogger logger,
        LogLevel logLevel,
        // ReSharper restore EntityNameCapturedOnly.Global
        EventId eventId,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger), nameof(logLevel))]
        in LogInterpolatedStringHandler message,
        ValueTuple dummy = default
    )
    {
        Diginsight.Logging.LoggerExtensions.Log(logger, logLevel, eventId, exception, in message);
    }

    [Obsolete("Moved to `Diginsight.Logging` namespace")]
#if NET9_0_OR_GREATER
    [OverloadResolutionPriority(-1)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Log(
        this ILogger logger,
        LogLevel logLevel,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger), nameof(logLevel))]
        in LogInterpolatedStringHandler message,
        ValueTuple dummy = default
    )
    {
        Log(logger, logLevel, default, exception, in message);
    }

    [Obsolete("Moved to `Diginsight.Logging` namespace")]
#if NET9_0_OR_GREATER
    [OverloadResolutionPriority(-1)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Log(
        this ILogger logger,
        LogLevel logLevel,
        [InterpolatedStringHandlerArgument(nameof(logger), nameof(logLevel))]
        in LogInterpolatedStringHandler message,
        ValueTuple dummy = default
    )
    {
        Log(logger, logLevel, null, in message);
    }
#endif
}
