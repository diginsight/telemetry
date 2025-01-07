#nullable enable
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
    public static void LogTrace(
        // ReSharper disable once EntityNameCapturedOnly.Global
        this ILogger logger,
        EventId eventId,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogTraceInterpolatedStringHandler message,
        ValueTuple dummy = default
    )
    {
        Diginsight.Logging.LoggerExtensions.LogTrace(logger, eventId, exception, in message);
    }
    
    [Obsolete("Moved to `Diginsight.Logging` namespace")]
#if NET9_0_OR_GREATER
    [OverloadResolutionPriority(-1)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogTrace(
        this ILogger logger,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogTraceInterpolatedStringHandler message,
        ValueTuple dummy = default
    )
    {
        LogTrace(logger, default, exception, in message);
    }
    
    [Obsolete("Moved to `Diginsight.Logging` namespace")]
#if NET9_0_OR_GREATER
    [OverloadResolutionPriority(-1)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogTrace(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogTraceInterpolatedStringHandler message,
        ValueTuple dummy = default
    )
    {
        LogTrace(logger, null, in message);
    }

    [Obsolete("Moved to `Diginsight.Logging` namespace")]
#if NET9_0_OR_GREATER
    [OverloadResolutionPriority(-1)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogDebug(
        // ReSharper disable once EntityNameCapturedOnly.Global
        this ILogger logger,
        EventId eventId,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogDebugInterpolatedStringHandler message,
        ValueTuple dummy = default
    )
    {
        Diginsight.Logging.LoggerExtensions.LogDebug(logger, eventId, exception, in message);
    }
    
    [Obsolete("Moved to `Diginsight.Logging` namespace")]
#if NET9_0_OR_GREATER
    [OverloadResolutionPriority(-1)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogDebug(
        this ILogger logger,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogDebugInterpolatedStringHandler message,
        ValueTuple dummy = default
    )
    {
        LogDebug(logger, default, exception, in message);
    }
    
    [Obsolete("Moved to `Diginsight.Logging` namespace")]
#if NET9_0_OR_GREATER
    [OverloadResolutionPriority(-1)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogDebug(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogDebugInterpolatedStringHandler message,
        ValueTuple dummy = default
    )
    {
        LogDebug(logger, null, in message);
    }

    [Obsolete("Moved to `Diginsight.Logging` namespace")]
#if NET9_0_OR_GREATER
    [OverloadResolutionPriority(-1)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogInformation(
        // ReSharper disable once EntityNameCapturedOnly.Global
        this ILogger logger,
        EventId eventId,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogInformationInterpolatedStringHandler message,
        ValueTuple dummy = default
    )
    {
        Diginsight.Logging.LoggerExtensions.LogInformation(logger, eventId, exception, in message);
    }
    
    [Obsolete("Moved to `Diginsight.Logging` namespace")]
#if NET9_0_OR_GREATER
    [OverloadResolutionPriority(-1)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogInformation(
        this ILogger logger,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogInformationInterpolatedStringHandler message,
        ValueTuple dummy = default
    )
    {
        LogInformation(logger, default, exception, in message);
    }
    
    [Obsolete("Moved to `Diginsight.Logging` namespace")]
#if NET9_0_OR_GREATER
    [OverloadResolutionPriority(-1)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogInformation(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogInformationInterpolatedStringHandler message,
        ValueTuple dummy = default
    )
    {
        LogInformation(logger, null, in message);
    }

    [Obsolete("Moved to `Diginsight.Logging` namespace")]
#if NET9_0_OR_GREATER
    [OverloadResolutionPriority(-1)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogWarning(
        // ReSharper disable once EntityNameCapturedOnly.Global
        this ILogger logger,
        EventId eventId,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogWarningInterpolatedStringHandler message,
        ValueTuple dummy = default
    )
    {
        Diginsight.Logging.LoggerExtensions.LogWarning(logger, eventId, exception, in message);
    }
    
    [Obsolete("Moved to `Diginsight.Logging` namespace")]
#if NET9_0_OR_GREATER
    [OverloadResolutionPriority(-1)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogWarning(
        this ILogger logger,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogWarningInterpolatedStringHandler message,
        ValueTuple dummy = default
    )
    {
        LogWarning(logger, default, exception, in message);
    }
    
    [Obsolete("Moved to `Diginsight.Logging` namespace")]
#if NET9_0_OR_GREATER
    [OverloadResolutionPriority(-1)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogWarning(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogWarningInterpolatedStringHandler message,
        ValueTuple dummy = default
    )
    {
        LogWarning(logger, null, in message);
    }

    [Obsolete("Moved to `Diginsight.Logging` namespace")]
#if NET9_0_OR_GREATER
    [OverloadResolutionPriority(-1)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogError(
        // ReSharper disable once EntityNameCapturedOnly.Global
        this ILogger logger,
        EventId eventId,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogErrorInterpolatedStringHandler message,
        ValueTuple dummy = default
    )
    {
        Diginsight.Logging.LoggerExtensions.LogError(logger, eventId, exception, in message);
    }
    
    [Obsolete("Moved to `Diginsight.Logging` namespace")]
#if NET9_0_OR_GREATER
    [OverloadResolutionPriority(-1)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogError(
        this ILogger logger,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogErrorInterpolatedStringHandler message,
        ValueTuple dummy = default
    )
    {
        LogError(logger, default, exception, in message);
    }
    
    [Obsolete("Moved to `Diginsight.Logging` namespace")]
#if NET9_0_OR_GREATER
    [OverloadResolutionPriority(-1)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogError(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogErrorInterpolatedStringHandler message,
        ValueTuple dummy = default
    )
    {
        LogError(logger, null, in message);
    }

    [Obsolete("Moved to `Diginsight.Logging` namespace")]
#if NET9_0_OR_GREATER
    [OverloadResolutionPriority(-1)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogCritical(
        // ReSharper disable once EntityNameCapturedOnly.Global
        this ILogger logger,
        EventId eventId,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogCriticalInterpolatedStringHandler message,
        ValueTuple dummy = default
    )
    {
        Diginsight.Logging.LoggerExtensions.LogCritical(logger, eventId, exception, in message);
    }
    
    [Obsolete("Moved to `Diginsight.Logging` namespace")]
#if NET9_0_OR_GREATER
    [OverloadResolutionPriority(-1)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogCritical(
        this ILogger logger,
        Exception? exception,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogCriticalInterpolatedStringHandler message,
        ValueTuple dummy = default
    )
    {
        LogCritical(logger, default, exception, in message);
    }
    
    [Obsolete("Moved to `Diginsight.Logging` namespace")]
#if NET9_0_OR_GREATER
    [OverloadResolutionPriority(-1)]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogCritical(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        in LogCriticalInterpolatedStringHandler message,
        ValueTuple dummy = default
    )
    {
        LogCritical(logger, null, in message);
    }

#endif
}
