using Diginsight.Logging;
using Diginsight.Options;
using Diginsight.Stringify;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Diginsight.Diagnostics;

public sealed class ActivityLifecycleLogEmitter : IActivityListenerLogic
{
    private static class CustomPropertyNames
    {
        public const string ExceptionPointers = nameof(ExceptionPointers);
        public const string EmittedStart = nameof(EmittedStart);
        public const string EmittedStop = nameof(EmittedStop);
    }

    public static readonly ActivityLifecycleLogEmitter Noop = new (
        NullLoggerFactory.Instance,
        new ClassAwareOptionsMonitorExtension<DiginsightActivitiesOptions>(
            new ClassAwareOptionsExtension<DiginsightActivitiesOptions>(
                Microsoft.Extensions.Options.Options.Create(new DiginsightActivitiesOptions { LogBehavior = LogBehavior.Hide }.Freeze())
            )
        )
    );

    private static readonly EventId StartActivityEventId = new (100, "StartActivity");
    private static readonly EventId StartMethodActivityEventId = new (110, "StartMethodActivity");
    private static readonly EventId EndActivityEventId = new (200, "EndActivity");
    private static readonly EventId EndMethodActivityEventId = new (210, "EndMethodActivity");

    private static readonly MethodInfo ExtractLoggableFromKvps_Method =
        typeof(ActivityLifecycleLogEmitter).GetMethod(nameof(ExtractLoggablesFromKvps), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private readonly ILoggerFactory loggerFactory;
    private readonly IClassAwareOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor;
    private IStringifyContextFactory? stringifyContextFactory;
    private readonly IActivityLoggingSampler? activityLoggingSampler;
    private readonly ILogger fallbackLogger;
    private readonly Func<nint> getExceptionPointers;
#if NET9_0_OR_GREATER
    private readonly Lock emittedLock = new ();
#else
    private readonly object emittedLock = new ();
#endif

    private IStringifyContextFactory StringifyContextFactory =>
        stringifyContextFactory ??= new StringifyContextFactoryBuilder().WithLoggerFactory(loggerFactory).Build();

    public ActivityLifecycleLogEmitter(
        ILoggerFactory loggerFactory,
        IClassAwareOptionsMonitor<DiginsightActivitiesOptions> activitiesOptionsMonitor,
        IStringifyContextFactory? stringifyContextFactory = null,
        IActivityLoggingSampler? activityLoggingSampler = null
    )
    {
        this.loggerFactory = loggerFactory;
        this.activitiesOptionsMonitor = activitiesOptionsMonitor;
        this.stringifyContextFactory = stringifyContextFactory;
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

    private bool IsEmitted(Activity activity, bool isStopped)
    {
        lock (emittedLock)
        {
            string customPropertyName = isStopped ? CustomPropertyNames.EmittedStop : CustomPropertyNames.EmittedStart;
            if (activity.GetCustomProperty(customPropertyName) is not null)
            {
                return true;
            }

            activity.SetCustomProperty(customPropertyName, new object());
            return false;
        }
    }

    [SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
    public void ActivityStarted(Activity activity)
    {
        if (IsEmitted(activity, false))
            return;

        string activityName = activity.OperationName;

        try
        {
            bool isStandalone;
            bool writeActionAsPrefix;
            bool disablePayloadRendering;
            ILogger textLogger;
            LogLevel logLevel;
            {
                ExtractLoggingInfo1(
                    activity,
                    out Type? callerType,
                    out IDiginsightActivitiesLogOptions activitiesOptions,
                    out LogBehavior behavior
                );

                activity.SetLogBehavior(behavior);

                if (behavior != LogBehavior.Show)
                    return;

                ExtractLoggingInfo2(
                    activity,
                    callerType,
                    activitiesOptions,
                    behavior,
                    false,
                    out isStandalone,
                    out writeActionAsPrefix,
                    out disablePayloadRendering,
                    out textLogger,
                    out logLevel
                );
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            string ComposeLogFormat(string format) => writeActionAsPrefix ? $"START {format}" : $"{format} START";

            activity.SetCustomProperty(CustomPropertyNames.ExceptionPointers, getExceptionPointers());

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
    public void ActivityStopped(Activity activity)
    {
        if (IsEmitted(activity, true))
            return;

        string activityName = activity.OperationName;

        try
        {
            bool isStandalone;
            bool writeActionAsPrefix;
            bool disablePayloadRendering;
            ILogger textLogger;
            LogLevel logLevel;
            {
                ExtractLoggingInfo1(
                    activity,
                    out Type? callerType,
                    out IDiginsightActivitiesLogOptions activitiesOptions,
                    out LogBehavior behavior
                );

                if (behavior != LogBehavior.Show)
                    return;

                ExtractLoggingInfo2(
                    activity,
                    callerType,
                    activitiesOptions,
                    behavior,
                    true,
                    out isStandalone,
                    out writeActionAsPrefix,
                    out disablePayloadRendering,
                    out textLogger,
                    out logLevel
                );
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            string ComposeLogFormat(string format) => writeActionAsPrefix ? $"END {format}" : $"{format} END";

            bool IsFaulted()
            {
                nint currentExceptionPointers = getExceptionPointers();
                return currentExceptionPointers != 0
                    && activity.GetCustomProperty(CustomPropertyNames.ExceptionPointers) is nint previousExceptionPointers
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

                string outputAsString0 = StringifyContextFactory.Stringify(output);

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
            ? (dict, string.Join(StringifyTokens.Separator2, dict.Select(static x => $"{x.Key}{StringifyTokens.Value}{x.Value}")))
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
        return kvps.ToDictionary(static x => x.Key, x => StringifyContextFactory.Stringify(x.Value));
    }

    private void ExtractLoggingInfo1(
        Activity activity,
        out Type? callerType,
        out IDiginsightActivitiesLogOptions activitiesOptions,
        out LogBehavior behavior
    )
    {
        callerType = activity.GetCallerType();

        activitiesOptions = activitiesOptionsMonitor.Get(callerType);
        LogBehavior candidateBehavior = activityLoggingSampler?.GetLogBehavior(activity) ?? activitiesOptions.LogBehavior;
        behavior = activity.Parent?.GetLogBehavior() == LogBehavior.Truncate ? LogBehavior.Truncate : candidateBehavior;
    }

    private void ExtractLoggingInfo2(
        Activity activity,
        Type? callerType,
        IDiginsightActivitiesLogOptions activitiesOptions,
        LogBehavior behavior,
        bool isStop,
        out bool isStandalone,
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

        isStandalone = activity.GetCustomProperty(ActivityCustomPropertyNames.IsStandalone) switch
        {
            bool b => b,
            null => true,
            _ => throw new InvalidOperationException($"Invalid '{ActivityCustomPropertyNames.IsStandalone}' in activity"),
        };

        ILogger MakeInnerLogger() => providedLogger ?? (callerType is not null ? loggerFactory.CreateLogger(callerType) : fallbackLogger);

        textLogger = behavior == LogBehavior.Show
            ? new ActivityLogger(MakeInnerLogger(), activity, isStop ? activity.Duration : null)
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
        private readonly ILogger decorateeWithMetadata;

        public ActivityLogger(ILogger decoratee, Activity activity, TimeSpan? duration)
        {
            Logging.ILogMetadata metadata = new LogMetadata(activity, duration);

            this.decoratee = decoratee;
            decorateeWithMetadata = decoratee.WithMetadata(metadata);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            using (SuppressInstrumentationScope.Begin())
            {
                decorateeWithMetadata.Log(logLevel, eventId, state, exception, formatter);
            }
        }

        public bool IsEnabled(LogLevel logLevel) => decoratee.IsEnabled(logLevel);

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
            => throw new NotSupportedException();

        private static class SuppressInstrumentationScope
        {
            static SuppressInstrumentationScope()
            {
                try
                {
                    BeginCore = (Func<bool, IDisposable>?)Type.GetType("OpenTelemetry.SuppressInstrumentationScope, OpenTelemetry, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7bd6737fe5b67e3c")
                        ?.GetMethod("Begin", BindingFlags.Public | BindingFlags.Static)
                        ?.CreateDelegate(typeof(Func<bool, IDisposable>), null);
                }
                catch (Exception)
                {
                    BeginCore = null;
                }
            }

            private static readonly Func<bool, IDisposable>? BeginCore;

            public static IDisposable? Begin() => BeginCore?.Invoke(true);
        }
    }

    public interface ILogMetadata : Logging.ILogMetadata
    {
        Activity Activity { get; }
        TimeSpan? Duration { get; }
    }

    private sealed class LogMetadata : ILogMetadata
    {
        public Activity Activity { get; }
        public TimeSpan? Duration { get; }

        public LogMetadata(Activity activity, TimeSpan? duration)
        {
            Activity = activity;
            Duration = duration;
        }
    }
}
