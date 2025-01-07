#nullable enable
#if NET7_0_OR_GREATER
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Diginsight.Logging;

[InterpolatedStringHandler]
public readonly struct LogTraceInterpolatedStringHandler
{
    private readonly LogInterpolatedStringHandler underlying;

    public LogTraceInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled)
    {
        underlying = new LogInterpolatedStringHandler(literalLength, formattedCount, logger, LogLevel.Trace, out isEnabled);
    }

    public void AppendLiteral(string value)
    {
        underlying.AppendLiteral(value);
    }

    public void AppendFormatted<T>(T? value)
    {
        underlying.AppendFormatted(value);
    }

    public void AppendFormatted<T>(T? value, int alignment)
    {
        underlying.AppendFormatted(value, alignment);
    }

    public void AppendFormatted<T>(T? value, int alignment, string format)
    {
        underlying.AppendFormatted(value, alignment, format);
    }

    public void AppendFormatted<T>(T? value, string format)
    {
        underlying.AppendFormatted(value, format);
    }

    internal void LogIfEnabled(EventId eventId, Exception? exception)
    {
        underlying.LogIfEnabled(eventId, exception);
    }
}

[InterpolatedStringHandler]
public readonly struct LogDebugInterpolatedStringHandler
{
    private readonly LogInterpolatedStringHandler underlying;

    public LogDebugInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled)
    {
        underlying = new LogInterpolatedStringHandler(literalLength, formattedCount, logger, LogLevel.Debug, out isEnabled);
    }

    public void AppendLiteral(string value)
    {
        underlying.AppendLiteral(value);
    }

    public void AppendFormatted<T>(T? value)
    {
        underlying.AppendFormatted(value);
    }

    public void AppendFormatted<T>(T? value, int alignment)
    {
        underlying.AppendFormatted(value, alignment);
    }

    public void AppendFormatted<T>(T? value, int alignment, string format)
    {
        underlying.AppendFormatted(value, alignment, format);
    }

    public void AppendFormatted<T>(T? value, string format)
    {
        underlying.AppendFormatted(value, format);
    }

    internal void LogIfEnabled(EventId eventId, Exception? exception)
    {
        underlying.LogIfEnabled(eventId, exception);
    }
}

[InterpolatedStringHandler]
public readonly struct LogInformationInterpolatedStringHandler
{
    private readonly LogInterpolatedStringHandler underlying;

    public LogInformationInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled)
    {
        underlying = new LogInterpolatedStringHandler(literalLength, formattedCount, logger, LogLevel.Information, out isEnabled);
    }

    public void AppendLiteral(string value)
    {
        underlying.AppendLiteral(value);
    }

    public void AppendFormatted<T>(T? value)
    {
        underlying.AppendFormatted(value);
    }

    public void AppendFormatted<T>(T? value, int alignment)
    {
        underlying.AppendFormatted(value, alignment);
    }

    public void AppendFormatted<T>(T? value, int alignment, string format)
    {
        underlying.AppendFormatted(value, alignment, format);
    }

    public void AppendFormatted<T>(T? value, string format)
    {
        underlying.AppendFormatted(value, format);
    }

    internal void LogIfEnabled(EventId eventId, Exception? exception)
    {
        underlying.LogIfEnabled(eventId, exception);
    }
}

[InterpolatedStringHandler]
public readonly struct LogWarningInterpolatedStringHandler
{
    private readonly LogInterpolatedStringHandler underlying;

    public LogWarningInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled)
    {
        underlying = new LogInterpolatedStringHandler(literalLength, formattedCount, logger, LogLevel.Warning, out isEnabled);
    }

    public void AppendLiteral(string value)
    {
        underlying.AppendLiteral(value);
    }

    public void AppendFormatted<T>(T? value)
    {
        underlying.AppendFormatted(value);
    }

    public void AppendFormatted<T>(T? value, int alignment)
    {
        underlying.AppendFormatted(value, alignment);
    }

    public void AppendFormatted<T>(T? value, int alignment, string format)
    {
        underlying.AppendFormatted(value, alignment, format);
    }

    public void AppendFormatted<T>(T? value, string format)
    {
        underlying.AppendFormatted(value, format);
    }

    internal void LogIfEnabled(EventId eventId, Exception? exception)
    {
        underlying.LogIfEnabled(eventId, exception);
    }
}

[InterpolatedStringHandler]
public readonly struct LogErrorInterpolatedStringHandler
{
    private readonly LogInterpolatedStringHandler underlying;

    public LogErrorInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled)
    {
        underlying = new LogInterpolatedStringHandler(literalLength, formattedCount, logger, LogLevel.Error, out isEnabled);
    }

    public void AppendLiteral(string value)
    {
        underlying.AppendLiteral(value);
    }

    public void AppendFormatted<T>(T? value)
    {
        underlying.AppendFormatted(value);
    }

    public void AppendFormatted<T>(T? value, int alignment)
    {
        underlying.AppendFormatted(value, alignment);
    }

    public void AppendFormatted<T>(T? value, int alignment, string format)
    {
        underlying.AppendFormatted(value, alignment, format);
    }

    public void AppendFormatted<T>(T? value, string format)
    {
        underlying.AppendFormatted(value, format);
    }

    internal void LogIfEnabled(EventId eventId, Exception? exception)
    {
        underlying.LogIfEnabled(eventId, exception);
    }
}

[InterpolatedStringHandler]
public readonly struct LogCriticalInterpolatedStringHandler
{
    private readonly LogInterpolatedStringHandler underlying;

    public LogCriticalInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled)
    {
        underlying = new LogInterpolatedStringHandler(literalLength, formattedCount, logger, LogLevel.Critical, out isEnabled);
    }

    public void AppendLiteral(string value)
    {
        underlying.AppendLiteral(value);
    }

    public void AppendFormatted<T>(T? value)
    {
        underlying.AppendFormatted(value);
    }

    public void AppendFormatted<T>(T? value, int alignment)
    {
        underlying.AppendFormatted(value, alignment);
    }

    public void AppendFormatted<T>(T? value, int alignment, string format)
    {
        underlying.AppendFormatted(value, alignment, format);
    }

    public void AppendFormatted<T>(T? value, string format)
    {
        underlying.AppendFormatted(value, format);
    }

    internal void LogIfEnabled(EventId eventId, Exception? exception)
    {
        underlying.LogIfEnabled(eventId, exception);
    }
}

#endif
