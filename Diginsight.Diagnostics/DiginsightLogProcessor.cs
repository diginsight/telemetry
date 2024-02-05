using Diginsight.Diagnostics.TextWriting;
using Diginsight.Strings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight.Diagnostics;

internal sealed class DiginsightLogProcessor : BaseProcessor<Activity>
{
    private static readonly MethodInfo ExtractLoggableFromKvps_Method =
        typeof(DiginsightLogProcessor).GetMethod(nameof(ExtractLoggablesFromKvps), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private readonly ILoggerFactory loggerFactory;
    private readonly IAppendingContextFactory appendingContextFactory;
    private readonly IDiginsightOptions diginsightOptions;
    private readonly IActivityRecordingSampler? activityRecordingSampler;
    private readonly ILogger fallbackLogger;

    public DiginsightLogProcessor(
        ILoggerFactory loggerFactory,
        IAppendingContextFactory appendingContextFactory,
        IOptions<DiginsightOptions> diginsightOptions,
        IActivityRecordingSampler? activityRecordingSampler = null
    )
    {
        this.loggerFactory = loggerFactory;
        this.appendingContextFactory = appendingContextFactory;
        this.diginsightOptions = diginsightOptions.Value;
        this.activityRecordingSampler = activityRecordingSampler;
        fallbackLogger = loggerFactory.CreateLogger($"{typeof(DiginsightLogProcessor).Namespace!}.$Activity");
    }

    public override void OnStart(Activity activity)
    {
        ExtractLoggingInfo(
            activity,
            diginsightOptions,
            out bool isStandalone,
            out bool shouldRecord,
            out ILogger textLogger,
            out ILogger otlpLogger,
            out LogLevel logLevel
        );

        if (!shouldRecord)
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }

        if (isStandalone)
        {
            textLogger.Log(logLevel, new EventId(100, "StartActivity"), "START {ActivityName}", activity.OperationName);
            return;
        }

        object? inputs = activity.GetCustomProperty(ActivityCustomPropertyNames.MakeInputs) switch
        {
            Func<object> makeObj => makeObj() ?? throw new InvalidOperationException("Invalid inputs in activity"),
            null => null,
            _ => throw new InvalidOperationException("Invalid inputs in activity"),
        };

        if (inputs is null)
        {
            textLogger.Log(logLevel, new EventId(110, "StartMethodActivity"), "START {ActivityName}()", activity.OperationName);
            return;
        }

        if (ExtractLoggable("Inputs", inputs) is not var (inputsAsDict, inputsAsString))
        {
            throw new InvalidOperationException("Invalid inputs in activity");
        }

        otlpLogger.Log(logLevel, new EventId(111, "MethodInputs"), inputsAsDict, null, (_, _) => $"Method inputs: {inputsAsString}");
        textLogger.Log(logLevel, new EventId(110, "StartMethodActivity"), "START {ActivityName}({Inputs})", activity.OperationName, inputsAsString);
    }

    public override void OnEnd(Activity activity)
    {
        ExtractLoggingInfo(
            activity,
            diginsightOptions,
            out bool isStandalone,
            out bool _,
            out ILogger textLogger,
            out ILogger otlpLogger,
            out LogLevel logLevel
        );

        if (isStandalone)
        {
            textLogger.Log(logLevel, new EventId(200, "EndActivity"), "END {ActivityName}", activity.OperationName);
            return;
        }

        string? outputAsString = LogOutput();
        string? namedOutputsAsString = LogNamedOutputs();

        string? LogOutput()
        {
            object? output;
            switch (activity.GetCustomProperty(ActivityCustomPropertyNames.Output))
            {
                case StrongBox<object?> outputBox:
                    output = outputBox.Value;
                    break;

                case null:
                    return null;

                default:
                    throw new InvalidOperationException("Invalid output in activity");
            }

            outputAsString = appendingContextFactory.MakeLogString(output);

            otlpLogger.Log(logLevel, new EventId(211, "MethodOutput"), new Dictionary<string, object?>() { ["Output"] = output }, null, (_, _) => $"Method output: {outputAsString}");

            return outputAsString;
        }

        string? LogNamedOutputs()
        {
            if (activity.GetCustomProperty(ActivityCustomPropertyNames.NamedOutputs) is not { } namedOutputs)
            {
                return null;
            }

            if (ExtractLoggable("NamedOutputs", namedOutputs) is not var (namedOutputAsDict, namedOutputsAsString0))
            {
                throw new InvalidOperationException("Invalid named outputs in activity");
            }

            otlpLogger.Log(logLevel, new EventId(212, "MethodNamedOutputs"), namedOutputAsDict, null, (_, _) => $"Method named outputs: {namedOutputsAsString0}");

            return namedOutputsAsString0;
        }

        switch (outputAsString, namedOutputsAsString)
        {
            case (null, null):
                textLogger.Log(logLevel, new EventId(210, "EndMethodActivity"), "END {ActivityName}()", activity.OperationName);
                break;

            case (not null, null):
                textLogger.Log(logLevel, new EventId(210, "EndMethodActivity"), "END {ActivityName}() => {Output}", activity.OperationName, outputAsString);
                break;

            case (null, not null):
                textLogger.Log(logLevel, new EventId(210, "EndMethodActivity"), "END {ActivityName}() [=> {NamedOutputs}]", activity.OperationName, namedOutputsAsString);
                break;

            case (not null, not null):
                textLogger.Log(logLevel, new EventId(210, "EndMethodActivity"), "END {ActivityName}() => {Output} [=> {NamedOutputs}]", activity.OperationName, outputAsString, namedOutputsAsString);
                break;
        }
    }

    private (IDictionary<string, object?>, string)? ExtractLoggable(string dictPrefix, object obj)
    {
        Type type = obj.GetType();
        if (type.IsAnonymous())
        {
            return ExtractLoggableFromAnonymous(dictPrefix, obj);
        }

        if (obj is Tags kvps)
        {
            return ExtractLoggablesFromKvps(dictPrefix, kvps);
        }

        if (type.IsIEnumerableOfKeyValuePair(out Type? tKey, out Type? tValue) && tKey == typeof(string))
        {
            return ((IDictionary<string, object?>, string))ExtractLoggableFromKvps_Method
                .MakeGenericMethod(tValue)
                .Invoke(this, [ dictPrefix, obj ])!;
        }

        return null;
    }

    private (IDictionary<string, object?>, string) ExtractLoggableFromAnonymous(string dictPrefix, object anonymous)
    {
        return ExtractLoggablesFromKvps(
            dictPrefix,
            anonymous.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new Tag(p.Name, p.GetValue(anonymous)))
        );
    }

    private (IDictionary<string, object?>, string) ExtractLoggablesFromKvps<TValue>(string dictPrefix, IEnumerable<KeyValuePair<string, TValue>> kvps)
    {
        IDictionary<string, object?> dict = new Dictionary<string, object?>();

        StringBuilder sb = new ();
        bool first = true;
        foreach (KeyValuePair<string, TValue> kvp in kvps)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                sb.Append(LogStringTokens.Separator2);
            }

            string key = kvp.Key;
            object? value = kvp.Value;
            string valueAsString = appendingContextFactory.MakeLogString(value);
            dict[$"{dictPrefix}.{key}"] = valueAsString;
            sb.Append($"{key}{LogStringTokens.Value}{valueAsString}");
        }

        return (dict, sb.ToString());
    }

    private void ExtractLoggingInfo(
        Activity activity,
        IDiginsightOptions diginsightOptions,
        out bool isStandalone,
        out bool shouldRecord,
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

        Type? callerType = activity.GetCallerType();

        isStandalone = activity.GetCustomProperty(ActivityCustomPropertyNames.IsStandalone) switch
        {
            bool b => b,
            null => true,
            _ => throw new InvalidOperationException($"Invalid '{ActivityCustomPropertyNames.IsStandalone}' in activity"),
        };

        bool? tempShouldRecord = activity.MatchesActivityNamePattern(diginsightOptions.NotRecordedActivityNames) ? false
            : activity.MatchesActivityNamePattern(diginsightOptions.RecordedActivityNames) ? true
            : null;
        activityRecordingSampler?.ShouldRecord(activity, callerType, ref tempShouldRecord);
        shouldRecord = tempShouldRecord ?? diginsightOptions.RecordActivities;

        ILogger innerLogger = providedLogger ?? (callerType is not null ? loggerFactory.CreateLogger(callerType) : fallbackLogger);

        textLogger = new ActivityLogger(innerLogger, activity.IsStopped ? activity.Duration : null);
        otlpLogger = shouldRecord ? new OtlpLogger(innerLogger) : NullLogger.Instance;

        logLevel = activity.GetCustomProperty(ActivityCustomPropertyNames.LogLevel) switch
        {
            LogLevel ll => ll,
            null => diginsightOptions.DefaultActivityLogLevel,
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
                    ActivityMark<TState>.For(state, duration),
                    exception,
                    (s, e) => formatter(s.State, e)
                );
            }
        }

        public bool IsEnabled(LogLevel logLevel) => decoratee.IsEnabled(logLevel);

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
            => throw new NotSupportedException();
    }

    private class ActivityMark<TState> : DiginsightTextWriter.IActivityMark<TState>
    {
        public TState State { get; }
        public TimeSpan? Duration { get; }

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
        object? DiginsightTextWriter.IActivityMark.State => State;
#endif

        public ActivityMark(TState state, TimeSpan? duration)
        {
            State = state;
            Duration = duration;
        }

        public static DiginsightTextWriter.IActivityMark<TState> For(TState state, TimeSpan? duration)
        {
            return state is Tags kvps ? new TagsActivityMark<TState>(state, kvps, duration) : new ActivityMark<TState>(state, duration);
        }
    }

    private sealed class TagsActivityMark<TState> : ActivityMark<TState>, Tags
    {
        private readonly Tags kvps;

        public TagsActivityMark(TState state, Tags kvps, TimeSpan? duration)
            : base(state, duration)
        {
            this.kvps = kvps;
        }

        public IEnumerator<Tag> GetEnumerator() => kvps.GetEnumerator();

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
            where TState : notnull
            => throw new NotSupportedException();
    }

    private sealed class OtlpOnly : DiginsightTextWriter.IOtlpOnly, Tags
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
