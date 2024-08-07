﻿using Diginsight.CAOptions;
using Diginsight.Strings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Diginsight.Diagnostics;

public sealed class ActivityLifecycleLogEmitter : IActivityListenerLogic
{
    private const string ExceptionPointersCustomPropertyName = "ExceptionPointers";

    private static readonly EventId StartActivityEventId = new (100, "StartActivity");
    private static readonly EventId StartMethodActivityEventId = new (110, "StartMethodActivity");
    private static readonly EventId EndActivityEventId = new (200, "EndActivity");
    private static readonly EventId EndMethodActivityEventId = new (210, "EndMethodActivity");

    private static readonly MethodInfo ExtractLoggableFromKvps_Method =
        typeof(ActivityLifecycleLogEmitter).GetMethod(nameof(ExtractLoggablesFromKvps), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private readonly ILoggerFactory loggerFactory;
    private readonly IAppendingContextFactory appendingContextFactory;
    private readonly IClassAwareOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor;
    private readonly IActivityLoggingSampler? activityLoggingSampler;
    private readonly ILogger fallbackLogger;
    private readonly Func<nint> getExceptionPointers;

    public ActivityLifecycleLogEmitter(
        ILoggerFactory loggerFactory,
        IAppendingContextFactory appendingContextFactory,
        IClassAwareOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor,
        IActivityLoggingSampler? activityLoggingSampler = null
    )
    {
        this.loggerFactory = loggerFactory;
        this.appendingContextFactory = appendingContextFactory;
        this.activitiesOptionsMonitor = activitiesOptionsMonitor;
        this.activityLoggingSampler = activityLoggingSampler;
        fallbackLogger = loggerFactory.CreateLogger($"{typeof(ActivityLifecycleLogEmitter).Namespace!}.$Activity");

#if NET
        getExceptionPointers = Marshal.GetExceptionPointers;
#else
        getExceptionPointers = (Func<nint>?)typeof(Marshal).GetMethod("GetExceptionPointers", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                ?.CreateDelegate(typeof(Func<nint>), null)
            ?? (static () => 0);
#endif
    }

    [SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
    void IActivityListenerLogic.ActivityStarted(Activity activity)
    {
        string activityName = activity.OperationName;

        try
        {
            ExtractLoggingInfo(
                activity,
                out bool isStandalone,
                out bool shouldLog,
                out bool writeActionAsPrefix,
                out bool disablePayloadRendering,
                out ILogger textLogger,
                out LogLevel logLevel
            );

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            string ComposeLogFormat(string format) => writeActionAsPrefix ? $"START {format}" : $"{format} START";

            if (!shouldLog)
                return;

            activity.SetCustomProperty(ExceptionPointersCustomPropertyName, getExceptionPointers());

            EventId eventId = isStandalone ? StartActivityEventId : StartMethodActivityEventId;
            if (disablePayloadRendering)
            {
                textLogger.Log(logLevel, eventId, ComposeLogFormat(isStandalone ? "{ActivityName}" : "{ActivityName}()"), activityName);
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
                textLogger.Log(logLevel, eventId, ComposeLogFormat(isStandalone ? "{ActivityName}" : "{ActivityName}()"), activityName);
                return;
            }

            if (ExtractLoggable(inputs) is not var (inputsAsDict, inputsAsString))
            {
                throw new InvalidOperationException("Invalid inputs in activity");
            }

            foreach (KeyValuePair<string, string> input in inputsAsDict)
            {
                activity.SetTag($"input.{input.Key}", input.Value);
            }

            textLogger.Log(logLevel, eventId, ComposeLogFormat("{ActivityName}({Inputs})"), activityName, inputsAsString);
        }
        catch (Exception exception)
        {
            fallbackLogger.LogWarning(exception, "Unhandled exception while logging start of activity {ActivityName}", activityName);
        }
    }

    [SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
    void IActivityListenerLogic.ActivityStopped(Activity activity)
    {
        string activityName = activity.OperationName;

        try
        {
            ExtractLoggingInfo(
                activity,
                out bool isStandalone,
                out bool shouldLog,
                out bool writeActionAsPrefix,
                out bool disablePayloadRendering,
                out ILogger textLogger,
                out LogLevel logLevel
            );

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            string ComposeLogFormat(string format) => writeActionAsPrefix ? $"END {format}" : $"{format} END";

            if (!shouldLog)
                return;

            bool IsFaulted()
            {
                nint currentExceptionPointers = getExceptionPointers();
                return currentExceptionPointers != 0
                    && activity.GetCustomProperty(ExceptionPointersCustomPropertyName) is nint previousExceptionPointers
                    && previousExceptionPointers != currentExceptionPointers;
            }

            bool faulted = IsFaulted();

            string? outputAsString = faulted || disablePayloadRendering ? null : LogOutput();
            string? namedOutputsAsString = faulted || disablePayloadRendering ? null : LogNamedOutputs();

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

                string outputAsString0 = appendingContextFactory.MakeLogString(output);

                activity.SetTag("output", outputAsString0);

                return outputAsString0;
            }

            string? LogNamedOutputs()
            {
                if (activity.GetCustomProperty(ActivityCustomPropertyNames.NamedOutputs) is not { } namedOutputs)
                    return null;

                if (ExtractLoggable(namedOutputs) is not var (namedOutputsAsDict, namedOutputsAsString0))
                    throw new InvalidOperationException("Invalid named outputs in activity");

                foreach (KeyValuePair<string, string> namedOutput in namedOutputsAsDict)
                {
                    activity.SetTag($"namedOutput.{namedOutput.Key}", namedOutput.Value);
                }

                return namedOutputsAsString0;
            }

            EventId eventId = isStandalone ? EndActivityEventId : EndMethodActivityEventId;
            switch (outputAsString, namedOutputsAsString)
            {
                case (null, null):
                    textLogger.Log(logLevel, eventId, ComposeLogFormat(faulted ? "{ActivityName} ‼" : "{ActivityName}"), activityName);
                    break;

                case (not null, null):
                    textLogger.Log(logLevel, eventId, ComposeLogFormat("{ActivityName} => {Output}"), activityName, outputAsString);
                    break;

                case (null, not null):
                    textLogger.Log(logLevel, eventId, ComposeLogFormat("{ActivityName} [=> {NamedOutputs}]"), activityName, namedOutputsAsString);
                    break;

                case (not null, not null):
                    textLogger.Log(logLevel, eventId, ComposeLogFormat("{ActivityName} => {Output} [=> {NamedOutputs}]"), activityName, outputAsString, namedOutputsAsString);
                    break;
            }
        }
        catch (Exception exception)
        {
            fallbackLogger.LogWarning(exception, "Unhandled exception while logging end of activity {ActivityName}", activityName);
        }
    }

    ActivitySamplingResult IActivityListenerLogic.Sample(ref ActivityCreationOptions<ActivityContext> creationOptions) => ActivitySamplingResult.AllData;

    private (IReadOnlyDictionary<string, string>, string)? ExtractLoggable(object obj)
    {
        Type type = obj.GetType();

        IReadOnlyDictionary<string, string>? dict;
        if (type.IsAnonymous())
        {
            dict = ExtractLoggableFromAnonymous(obj);
        }
        else if (obj is Tags kvps)
        {
            dict = ExtractLoggablesFromKvps(kvps);
        }
        else if (type.IsIEnumerableOfKeyValuePair(out Type? tKey, out Type? tValue) && tKey == typeof(string))
        {
            dict = (IReadOnlyDictionary<string, string>)ExtractLoggableFromKvps_Method
                .MakeGenericMethod(tValue)
                .Invoke(this, [ obj ])!;
        }
        else
        {
            dict = null;
        }

        return dict is not null
            ? (dict, string.Join(LogStringTokens.Separator2, dict.Select(static x => $"{x.Key}{LogStringTokens.Value}{x.Value}")))
            : null;
    }

    private IReadOnlyDictionary<string, string> ExtractLoggableFromAnonymous(object anonymous)
    {
        return ExtractLoggablesFromKvps(
            anonymous.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new Tag(p.Name, p.GetValue(anonymous)))
        );
    }

    private IReadOnlyDictionary<string, string> ExtractLoggablesFromKvps<TValue>(IEnumerable<KeyValuePair<string, TValue>> kvps)
    {
        return kvps.ToDictionary(static x => x.Key, x => appendingContextFactory.MakeLogString(x.Value));
    }

    private void ExtractLoggingInfo(
        Activity activity,
        out bool isStandalone,
        out bool shouldLog,
        out bool writeActionAsPrefix,
        out bool disablePayloadRendering,
        out ILogger textLogger,
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

        ILogger MakeInnerLogger() => providedLogger ?? (callerType is not null ? loggerFactory.CreateLogger(callerType) : fallbackLogger);

        IDiginsightActivitiesLogOptions activitiesOptions = activitiesOptionsMonitor.Get(callerType);
        shouldLog = activityLoggingSampler?.ShouldLog(activity) ?? activitiesOptions.LogActivities;
        textLogger = shouldLog
            ? new ActivityLogger(MakeInnerLogger(), activity)
            : NullLogger.Instance;

        writeActionAsPrefix = activitiesOptions.WriteActivityActionAsPrefix;
        disablePayloadRendering = activitiesOptions.DisablePayloadRendering;

        logLevel = activity.GetCustomProperty(ActivityCustomPropertyNames.LogLevel) switch
        {
            LogLevel ll => ll,
            null => activitiesOptions.ActivityLogLevel,
            _ => throw new InvalidOperationException("Invalid log level in activity"),
        };
    }

    private sealed class ActivityLogger : ILogger
    {
        private readonly ILogger decoratee;
        private readonly Activity activity;
        private readonly TimeSpan? duration;

        public ActivityLogger(ILogger decoratee, Activity activity)
        {
            this.decoratee = decoratee;
            this.activity = activity;
            duration = activity.IsStopped ? activity.Duration : null;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            using (SuppressInstrumentationScope.Begin())
            {
                decoratee.Log(
                    logLevel,
                    eventId,
                    ActivityMark<TState>.For(state, duration, activity),
                    exception,
                    (s, e) => formatter(s.State, e)
                );
            }
        }

        public bool IsEnabled(LogLevel logLevel) => decoratee.IsEnabled(logLevel);

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
            => throw new NotSupportedException();

        private static class SuppressInstrumentationScope
        {
            private static readonly Func<bool, IDisposable>? BeginCore =
                (Func<bool, IDisposable>?)Type.GetType("OpenTelemetry.SuppressInstrumentationScope, OpenTelemetry, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7bd6737fe5b67e3c")
                    ?.GetMethod("Begin", BindingFlags.Public | BindingFlags.Static)
                    ?.CreateDelegate(typeof(Func<bool, IDisposable>), null);

            public static IDisposable? Begin() => BeginCore?.Invoke(true);
        }
    }

    public interface IActivityMark
    {
        object? State { get; }
        TimeSpan? Duration { get; }
        Activity Activity { get; }
    }

    public interface IActivityMark<out TState> : IActivityMark
    {
        new TState State { get; }

#if NET || NETSTANDARD2_1_OR_GREATER
        object? IActivityMark.State => State;
#endif
    }

    private class ActivityMark<TState> : IActivityMark<TState>
    {
        public TState State { get; }
        public TimeSpan? Duration { get; }
        public Activity Activity { get; }

#if !(NET || NETSTANDARD2_1_OR_GREATER)
        object? IActivityMark.State => State;
#endif

        public ActivityMark(TState state, TimeSpan? duration, Activity activity)
        {
            State = state;
            Duration = duration;
            Activity = activity;
        }

        public static IActivityMark<TState> For(TState state, TimeSpan? duration, Activity activity)
        {
            return state is Tags kvps
                ? new TagsActivityMark<TState>(state, kvps, duration, activity)
                : new ActivityMark<TState>(state, duration, activity);
        }
    }

    private sealed class TagsActivityMark<TState> : ActivityMark<TState>, Tags
    {
        private readonly Tags kvps;

        public TagsActivityMark(TState state, Tags kvps, TimeSpan? duration, Activity activity)
            : base(state, duration, activity)
        {
            this.kvps = kvps;
        }

        public IEnumerator<Tag> GetEnumerator() => kvps.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
