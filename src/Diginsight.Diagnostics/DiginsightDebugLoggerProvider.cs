using Diginsight.Diagnostics.TextWriting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace Diginsight.Diagnostics;

[ProviderAlias("DiginsightDebug")]
public sealed class DiginsightDebugLoggerProvider : ILoggerProvider
{
    private readonly IEnumerable<ILineTokenParser> lineTokenParsers;
    private readonly IDiginsightDebugLoggerOptions options;
    private readonly TimeProvider timeProvider;

    private readonly ConcurrentDictionary<string, ILogger> loggers = new ();
    private LineDescriptor? lineDescriptor;

    private LineDescriptor LineDescriptor => lineDescriptor ??= LineDescriptor.ParseFull(options.Pattern, lineTokenParsers);

    public DiginsightDebugLoggerProvider(
        IEnumerable<ILineTokenParser> lineTokenParsers,
        IOptions<DiginsightDebugLoggerOptions> options,
        TimeProvider? timeProvider = null
    )
    {
        this.lineTokenParsers = lineTokenParsers;
        this.options = options.Value;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return loggers.GetOrAdd(
            categoryName,
#if NET || NETSTANDARD2_1_OR_GREATER
            static (k, a) => new Logger(k, a),
            this
#else
            k => new Logger(k, this)
#endif
        );
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    private sealed class Logger : ILogger
    {
        private readonly string category;
        private readonly DiginsightDebugLoggerProvider owner;

        public Logger(string category, DiginsightDebugLoggerProvider owner)
        {
            this.category = category;
            this.owner = owner;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            object? state0 = state;
            DiginsightTextWriter.ExpandState(
                ref state0,
                out bool isActivity,
                out TimeSpan? duration,
                out DateTimeOffset? maybeTimestamp,
                out Activity? activity,
                out Func<LineDescriptor, LineDescriptor>? sealLineDescriptor
            );

            DiginsightTextWriter.Write(
                DebugTextWriter.Instance,
                false,
                TimeZoneInfo.ConvertTime(maybeTimestamp ?? owner.timeProvider.GetUtcNow(), owner.options.TimeZone ?? TimeZoneInfo.Local),
                activity,
                logLevel,
                category,
                formatter(state, exception),
                exception,
                isActivity,
                duration,
                owner.LineDescriptor,
                sealLineDescriptor
            );
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel < LogLevel.None;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }

    private sealed class DebugTextWriter : TextWriter
    {
        public static readonly TextWriter Instance = new DebugTextWriter();

        public override Encoding Encoding { get; } = new UTF8Encoding(false);

        private DebugTextWriter() { }

        public override void Write(char value)
        {
            Debug.Write(value.ToString());
        }

        public override void Write(string? value)
        {
            Debug.Write(value);
        }

        public override void Write(object? value)
        {
            Debug.Write(value);
        }

        public override void WriteLine(string? value)
        {
            Debug.WriteLine(value);
        }

        public override void Write(string format, object? arg0)
        {
            Debug.WriteLine(format, arg0);
        }

        public override void Write(string format, object? arg0, object? arg1)
        {
            Debug.WriteLine(format, arg0, arg1);
        }

        public override void Write(string format, object? arg0, object? arg1, object? arg2)
        {
            Debug.WriteLine(format, arg0, arg1, arg2);
        }

        public override void Write(string format, params object?[] arg)
        {
            Debug.WriteLine(format, arg);
        }

        public override void WriteLine(object? value)
        {
            Debug.WriteLine(value);
        }
    }
}
