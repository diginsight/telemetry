#nullable enable
#if NET7_0_OR_GREATER
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
#endif

namespace Diginsight.Logging;

public static partial class LoggerExtensions
{
#if NET7_0_OR_GREATER
    public static void LogTrace(
        // ReSharper disable once EntityNameCapturedOnly.Global
        this ILogger logger,
        EventId eventId,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogTraceInterpolatedStringHandler message
    )
    {
        message.LogIfEnabled(eventId, exception);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogTrace(
        this ILogger logger,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogTraceInterpolatedStringHandler message
    )
    {
        LogTrace(logger, default, exception, in message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogTrace(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogTraceInterpolatedStringHandler message
    )
    {
        LogTrace(logger, null, in message);
    }

    public static void LogDebug(
        // ReSharper disable once EntityNameCapturedOnly.Global
        this ILogger logger,
        EventId eventId,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogDebugInterpolatedStringHandler message
    )
    {
        message.LogIfEnabled(eventId, exception);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogDebug(
        this ILogger logger,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogDebugInterpolatedStringHandler message
    )
    {
        LogDebug(logger, default, exception, in message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogDebug(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogDebugInterpolatedStringHandler message
    )
    {
        LogDebug(logger, null, in message);
    }

    public static void LogInformation(
        // ReSharper disable once EntityNameCapturedOnly.Global
        this ILogger logger,
        EventId eventId,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogInformationInterpolatedStringHandler message
    )
    {
        message.LogIfEnabled(eventId, exception);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogInformation(
        this ILogger logger,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogInformationInterpolatedStringHandler message
    )
    {
        LogInformation(logger, default, exception, in message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogInformation(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogInformationInterpolatedStringHandler message
    )
    {
        LogInformation(logger, null, in message);
    }

    public static void LogWarning(
        // ReSharper disable once EntityNameCapturedOnly.Global
        this ILogger logger,
        EventId eventId,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogWarningInterpolatedStringHandler message
    )
    {
        message.LogIfEnabled(eventId, exception);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogWarning(
        this ILogger logger,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogWarningInterpolatedStringHandler message
    )
    {
        LogWarning(logger, default, exception, in message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogWarning(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogWarningInterpolatedStringHandler message
    )
    {
        LogWarning(logger, null, in message);
    }

    public static void LogError(
        // ReSharper disable once EntityNameCapturedOnly.Global
        this ILogger logger,
        EventId eventId,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogErrorInterpolatedStringHandler message
    )
    {
        message.LogIfEnabled(eventId, exception);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogError(
        this ILogger logger,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogErrorInterpolatedStringHandler message
    )
    {
        LogError(logger, default, exception, in message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogError(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogErrorInterpolatedStringHandler message
    )
    {
        LogError(logger, null, in message);
    }

    public static void LogCritical(
        // ReSharper disable once EntityNameCapturedOnly.Global
        this ILogger logger,
        EventId eventId,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogCriticalInterpolatedStringHandler message
    )
    {
        message.LogIfEnabled(eventId, exception);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogCritical(
        this ILogger logger,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogCriticalInterpolatedStringHandler message
    )
    {
        LogCritical(logger, default, exception, in message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogCritical(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogCriticalInterpolatedStringHandler message
    )
    {
        LogCritical(logger, null, in message);
    }

#endif
}
