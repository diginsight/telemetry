using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
#if !NET7_0_OR_GREATER
using System.Reflection;
#endif

namespace Diginsight.Diagnostics;

public static class ObservabilityExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? StartMethodActivity<T>(
        this ActivitySource source,
        ILogger<T> logger,
        object inputs,
        [CallerMemberName] string callerMemberName = "",
        LogLevel logLevel = LogLevel.Debug,
        ActivityKind activityKind = ActivityKind.Internal
    )
    {
        return source.StartMethodActivity(typeof(T), logger, () => inputs, callerMemberName, logLevel, activityKind);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? StartMethodActivity<T>(
        this ActivitySource source,
        ILogger<T> logger,
        Func<object?>? makeInputs = null,
        [CallerMemberName] string callerMemberName = "",
        LogLevel logLevel = LogLevel.Debug,
        ActivityKind activityKind = ActivityKind.Internal
    )
    {
        return source.StartMethodActivity(typeof(T), logger, makeInputs, callerMemberName, logLevel, activityKind);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? StartMethodActivity(
        this ActivitySource source,
        Type callerType,
        ILogger logger,
        object inputs,
        [CallerMemberName] string callerMemberName = "",
        LogLevel logLevel = LogLevel.Debug,
        ActivityKind activityKind = ActivityKind.Internal
    )
    {
        return source.StartMethodActivity(callerType, logger, () => inputs, callerMemberName, logLevel, activityKind);
    }

    public static Activity? StartMethodActivity(
        this ActivitySource source,
        Type callerType,
        ILogger logger,
        Func<object?>? makeInputs = null,
        [CallerMemberName] string callerMemberName = "",
        LogLevel logLevel = LogLevel.Debug,
        ActivityKind activityKind = ActivityKind.Internal
    )
    {
        Activity? activity = source.CreateActivity($"{callerType.Name}.{callerMemberName}", activityKind);
        if (activity is null)
        {
            return null;
        }

        activity.SetCustomProperty(ActivityCustomPropertyNames.Logger, logger);
        activity.SetCustomProperty(ActivityCustomPropertyNames.LogLevel, logLevel);
        activity.SetCustomProperty(ActivityCustomPropertyNames.Inputs, makeInputs);

        activity.Start();

        return activity;
    }

    public static void StoreOutput(this ILogger logger, object? output)
    {
        if (Activity.Current is not { } activity)
        {
            return;
        }

        activity.SetCustomProperty(ActivityCustomPropertyNames.Logger, logger);
        activity.SetCustomProperty(ActivityCustomPropertyNames.Output, new StrongBox<object?>(output));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimerHistogram CreateTimer(this Meter meter, string name, string? unit = "ms", string? description = null)
    {
        return new TimerHistogram(meter, name, unit, description);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .Configure<ObservabilityOptions>(configuration);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ILoggingBuilder AddObservability(this ILoggingBuilder loggingBuilder)
    {
        return loggingBuilder
            .AddConsoleFormatter<ObservabilityConsoleFormatter, ObservabilityConsoleFormatterOptions>()
            .AddConsole(
                static consoleLoggerOptions => { consoleLoggerOptions.FormatterName = ObservabilityConsoleFormatter.FormatterName; }
            )
            .Configure(
                static loggerFactoryOptions => { loggerFactoryOptions.ActivityTrackingOptions = ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.TraceFlags; }
            )
            .AddOpenTelemetry(
                static openTelemetryLoggerOptions =>
                {
                    openTelemetryLoggerOptions.IncludeFormattedMessage = true;
                    openTelemetryLoggerOptions.IncludeScopes = true;
                }
            );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MeterProviderBuilder AddObservability(this MeterProviderBuilder meterProviderBuilder)
    {
        return meterProviderBuilder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TracerProviderBuilder AddObservability(this TracerProviderBuilder tracerProviderBuilder)
    {
        return tracerProviderBuilder
            .AddProcessor<ObservabilityLogProcessor>();
    }

    public static MeterProviderBuilder AddViews(
        this MeterProviderBuilder builder,
        params (string InstrumentName, MetricStreamConfiguration MetricStreamConfiguration)[] views
    )
    {
        foreach (var (instrumentName, metricStreamConfiguration) in views)
        {
            builder.AddView(instrumentName, metricStreamConfiguration);
        }

        return builder;
    }

    public static MeterProviderBuilder AddMetrics<T>(this MeterProviderBuilder builder)
#if NET7_0_OR_GREATER
        where T : ICustomMetrics<T>
#else
        where T : CustomMetrics
#endif
    {
#if NET7_0_OR_GREATER
        builder.AddMeter(T.ObservabilityName);
        builder.AddViews(T.Views);
#else
        T customMetrics = (T)typeof(T).GetField("Instance", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        builder.AddMeter(customMetrics.ObservabilityName);
        builder.AddViews(customMetrics.Views);
#endif
        return builder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordDurationMetric(this Activity? activity, Histogram<long> durationMetric, params Tag[] tags)
    {
        activity.RecordDurationMetric((object)durationMetric, tags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordDurationMetric(this Activity? activity, Histogram<double> durationMetric, params Tag[] tags)
    {
        activity.RecordDurationMetric((object)durationMetric, tags);
    }

    private static void RecordDurationMetric(this Activity? activity, object durationMetric, params Tag[] tags)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetCustomProperty(ActivityCustomPropertyNames.DurationMetric, durationMetric);
        activity.SetCustomProperty(ActivityCustomPropertyNames.DurationMetricTags, tags);
    }

    public static void AddDurationMetricTags(this Activity? activity, params Tag[] tags)
    {
        if (activity is null)
        {
            return;
        }

        if (activity.GetCustomProperty(ActivityCustomPropertyNames.DurationMetric) is not (Histogram<double> or Histogram<long>))
        {
            throw new ArgumentException("Activity has no associated duration metric");
        }

        Tag[] allTags = tags
            .Concat(activity.GetDurationMetricTags())
#if NET6_0_OR_GREATER
            .DistinctBy(static x => x.Key)
#else
            .Distinct(TagKeyComparer.Instance)
#endif
            .ToArray();
        activity.SetCustomProperty(ActivityCustomPropertyNames.DurationMetricTags, allTags);
    }

#if !NET6_0_OR_GREATER
    private sealed class TagKeyComparer : IEqualityComparer<Tag>
    {
        public static readonly IEqualityComparer<Tag> Instance = new TagKeyComparer();

        private TagKeyComparer() { }

        public bool Equals(Tag x1, Tag x2)
        {
            return string.Equals(x1.Key, x2.Key);
        }

        public int GetHashCode(Tag x)
        {
            return x.Key.GetHashCode();
        }
    }
#endif

    internal static Tag[] GetDurationMetricTags(this Activity activity)
    {
        return activity.GetCustomProperty(ActivityCustomPropertyNames.DurationMetricTags) switch
        {
            Tag[] tags => tags,
            null => Array.Empty<Tag>(),
            _ => throw new InvalidOperationException("Invalid duration metric tags in activity"),
        };
    }
}
