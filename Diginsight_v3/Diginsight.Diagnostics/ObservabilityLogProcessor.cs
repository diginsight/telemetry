using Diginsight.Strings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight.Diagnostics;

internal sealed class ObservabilityLogProcessor : BaseProcessor<Activity>
{
    private readonly ILogger<ObservabilityLogProcessor> logger;
    private readonly ILogStringComposer logStringComposer;
    private readonly IOptionsMonitor<ObservabilityOptions> observabilityOptionsMonitor;

    public ObservabilityLogProcessor(
        ILogger<ObservabilityLogProcessor> logger,
        ILogStringComposer logStringComposer,
        IOptionsMonitor<ObservabilityOptions> observabilityOptionsMonitor
    )
    {
        this.logger = logger;
        this.logStringComposer = logStringComposer;
        this.observabilityOptionsMonitor = observabilityOptionsMonitor;
    }

    public override void OnStart(Activity activity)
    {
        ExtractLoggingInfo(
            activity,
            observabilityOptionsMonitor.CurrentValue,
            out bool isStandalone,
            out ILogger textLogger,
            out ILogger otlpLogger,
            out LogLevel logLevel
        );

        if (isStandalone)
        {
            textLogger.Log(logLevel, "{ActivityName} START", activity.OperationName);
        }

        object? inputs = activity.GetCustomProperty(ActivityCustomPropertyNames.Inputs) switch
        {
            Func<object?> makeInputs => makeInputs(),
            null => null,
            _ => throw new InvalidOperationException("Invalid inputs in activity"),
        };

        string inputsAsString;
        IDictionary<string, string>? inputsAsDict;
        if (inputs is null)
        {
            inputsAsString = "";
            inputsAsDict = null;
        }
        else if (!inputs.GetType().IsAnonymous())
        {
            throw new InvalidOperationException("Invalid inputs in activity");
        }
        else
        {
            inputsAsDict = new Dictionary<string, string>();

            StringBuilder sb = new ();
            bool first = true;
            foreach (PropertyInfo property in inputs.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(LogStringTokens.Separator2);
                }

                string propertyName = property.Name;
                string propertyValue = logStringComposer.MakeLogString(property.GetValue(inputs));

                inputsAsDict[$"Inputs.{propertyName}"] = propertyValue;
                sb.Append($"{propertyName}{LogStringTokens.Value}{propertyValue}");
            }

            inputsAsString = sb.ToString();
        }

        textLogger.Log(logLevel, "{ActivityName}({Inputs}) START", activity.OperationName, inputsAsString);

        if (inputsAsDict is not null)
        {
            otlpLogger.Log(
                logLevel,
                default,
                inputsAsDict,
                null,
                (_, _) => $"Method inputs: {inputsAsString}"
            );
        }
    }

    public override void OnEnd(Activity activity)
    {
        ExtractLoggingInfo(
            activity,
            observabilityOptionsMonitor.CurrentValue,
            out bool isStandalone,
            out ILogger textLogger,
            out ILogger otlpLogger,
            out LogLevel logLevel
        );

        if (isStandalone)
        {
            textLogger.Log(logLevel, "{ActivityName} END", activity.OperationName);
            return;
        }

        object? output;
        switch (activity.GetCustomProperty(ActivityCustomPropertyNames.Output))
        {
            case StrongBox<object?> outputBox:
                output = outputBox.Value;
                break;

            case null:
                textLogger.Log(logLevel, "{ActivityName}() END", activity.OperationName);
                return;

            default:
                throw new InvalidOperationException("Invalid output in activity");
        }

        string outputAsString = logStringComposer.MakeLogString(output);

        otlpLogger.Log(
            logLevel,
            default,
            new Dictionary<string, object?>() { ["Output"] = output },
            null,
            (_, _) => $"Method output: {outputAsString}"
        );

        textLogger.Log(logLevel, "{ActivityName}() END returned => {Output}", activity.OperationName, outputAsString);
    }

    private void ExtractLoggingInfo(
        Activity activity,
        IObservabilityOptions observabilityOptions,
        out bool isStandalone,
        out ILogger textLogger,
        out ILogger otlpLogger,
        out LogLevel logLevel
    )
    {
        ILogger? providedLogger = activity.GetCustomProperty(ActivityCustomPropertyNames.Logger) switch
        {
            ILogger l => l,
            null => null,
            _ => throw new InvalidOperationException("Invalid logger in activity"),
        };

        isStandalone = providedLogger is null;
        ILogger innerLogger = providedLogger ?? logger;

        textLogger = new ActivityLogger(innerLogger, activity.IsStopped ? activity.Duration : null);
        otlpLogger = new OtlpLogger(innerLogger);

        logLevel = activity.GetCustomProperty(ActivityCustomPropertyNames.LogLevel) switch
        {
            LogLevel ll => ll,
            null => observabilityOptions.DefaultActivityLogLevel,
            _ => throw new InvalidOperationException("Invalid log level in activity"),
        };
    }

    private sealed class ActivityLogger : ILogger
    {
        private readonly ILogger decoratee;
        private readonly TimeSpan? duration;

        public ActivityLogger(ILogger decoratee, TimeSpan? duration = null)
        {
            this.decoratee = decoratee;
            this.duration = duration;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            using (SuppressInstrumentationScope.Begin())
            {
                decoratee.Log(
                    logLevel,
                    eventId,
                    new ActivityMark<TState>(state, duration),
                    exception,
                    (s, e) => formatter(s.State, e)
                );
            }
        }

        public bool IsEnabled(LogLevel logLevel) => decoratee.IsEnabled(logLevel);

        public IDisposable BeginScope<TState>(TState state)
#if NET7_0_OR_GREATER
            where TState : notnull
#endif
            => throw new NotSupportedException();
    }

    private sealed class ActivityMark<TState> : ObservabilityTextWriter.IActivityMark, Tags
    {
        public TState State { get; }
        public TimeSpan? Duration { get; }

        public ActivityMark(TState state, TimeSpan? duration)
        {
            State = state;
            Duration = duration;
        }

        public IEnumerator<Tag> GetEnumerator() => (State as Tags ?? Enumerable.Empty<Tag>()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class OtlpLogger : ILogger
    {
        private readonly ILogger decoratee;

        public OtlpLogger(ILogger decoratee)
        {
            this.decoratee = decoratee;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            decoratee.Log(
                logLevel,
                eventId,
                new OtlpOnly((IDictionary<string, object?>)state!),
                exception,
                (s, e) => formatter((TState)s.State, e)
            );
        }

        public bool IsEnabled(LogLevel logLevel) => decoratee.IsEnabled(logLevel);

        public IDisposable BeginScope<TState>(TState state)
#if NET7_0_OR_GREATER
            where TState : notnull
#endif
            => throw new NotSupportedException();
    }

    private sealed class OtlpOnly : ObservabilityTextWriter.IOtlpOnly, Tags
    {
        public Tags State { get; }

        public OtlpOnly(IDictionary<string, object?> state)
        {
            State = state;
        }

        public IEnumerator<Tag> GetEnumerator() => State.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
