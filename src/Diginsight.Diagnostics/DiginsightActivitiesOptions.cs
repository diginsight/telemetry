using Diginsight.Options;
using Microsoft.Extensions.Logging;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents configuration options for Diginsight activities, activity lifecycle logging, and span duration metric recording.
/// </summary>
public sealed class DiginsightActivitiesOptions
    : IDiginsightActivitiesOptions,
        IDiginsightActivitiesLogOptions,
        IMetricRecordingOptions,
        IDynamicallyConfigurable,
        IVolatilelyConfigurable
{
    private readonly bool frozen;

    private LogBehavior logBehavior = LogBehavior.Hide;
    private LogLevel activityLogLevel = LogLevel.Debug;
    private bool writeActivityActionAsPrefix;
    private bool enablePayloadLogging;
    private bool enablePayloadTagging;
    private bool recordSpanDuration;
    private string? meterName;
    private string? spanDurationMeterName;
    private string? spanDurationMetricName;
    private string? spanDurationMetricDescription;

    /// <summary>
    /// Gets the activity source name patterns mapped to listener enablement values.
    /// </summary>
    public IDictionary<string, bool> ActivitySources { get; }

    IReadOnlyDictionary<string, bool> IDiginsightActivitiesOptions.ActivitySources => (IReadOnlyDictionary<string, bool>)ActivitySources;

    /// <summary>
    /// Gets the activity name patterns mapped to activity lifecycle logging behavior.
    /// </summary>
    public IDictionary<string, LogBehavior> LoggedActivityNames { get; }

    IReadOnlyDictionary<string, LogBehavior> IDiginsightActivitiesLogOptions.ActivityNames => (IReadOnlyDictionary<string, LogBehavior>)LoggedActivityNames;

    /// <summary>
    /// Gets the default activity lifecycle logging behavior.
    /// </summary>
    public LogBehavior LogBehavior
    {
        get => logBehavior;
        set => logBehavior = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    /// <summary>
    /// Gets the activity lifecycle log level.
    /// </summary>
    public LogLevel ActivityLogLevel
    {
        get => activityLogLevel;
        set => activityLogLevel = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    LogLevel IDiginsightActivitiesLogOptions.LogLevel => ActivityLogLevel;

    /// <summary>
    /// Gets a value indicating whether activity lifecycle log actions are written before the activity name.
    /// </summary>
    public bool WriteActivityActionAsPrefix
    {
        get => writeActivityActionAsPrefix;
        set => writeActivityActionAsPrefix = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    /// <summary>
    /// Gets a value indicating whether activity input and output payloads are written to lifecycle logs.
    /// </summary>
    public bool EnablePayloadLogging
    {
        get => enablePayloadLogging;
        set => enablePayloadLogging = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    /// <summary>
    /// Gets a value indicating whether activity input and output payloads are added as activity tags.
    /// </summary>
    public bool EnablePayloadTagging
    {
        get => enablePayloadTagging;
        set => enablePayloadTagging = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    /// <summary>
    /// Gets a value indicating whether span duration metrics are recorded.
    /// </summary>
    public bool RecordSpanDuration
    {
        get => recordSpanDuration;
        set => recordSpanDuration = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    bool IMetricRecordingOptions.Record => RecordSpanDuration;

    /// <summary>
    /// Gets the meter name used for span duration metric recording.
    /// </summary>
    public string? SpanDurationMeterName
    {
        get => spanDurationMeterName;
        set => spanDurationMeterName = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    string IMetricRecordingOptions.MeterName =>
        SpanDurationMeterName ?? throw new InvalidOperationException($"{nameof(IMetricRecordingOptions.MeterName)} is unset");

    /// <summary>
    /// Gets the span duration metric name.
    /// </summary>
    public string? SpanDurationMetricName
    {
        get => spanDurationMetricName;
        set => spanDurationMetricName = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    string IMetricRecordingOptions.MetricName => SpanDurationMetricName ?? "diginsight.span_duration";

    /// <summary>
    /// Gets the span duration metric description.
    /// </summary>
    public string? SpanDurationMetricDescription
    {
        get => spanDurationMetricDescription;
        set => spanDurationMetricDescription = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    string? IMetricRecordingOptions.MetricDescription => SpanDurationMetricDescription;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiginsightActivitiesOptions" /> class with default configuration.
    /// </summary>
    public DiginsightActivitiesOptions()
        : this(
            false,
            new Dictionary<string, bool>(),
            new Dictionary<string, LogBehavior>()
        ) { }

    private DiginsightActivitiesOptions(
        bool frozen,
        IDictionary<string, bool> activitySources,
        IDictionary<string, LogBehavior> loggedActivityNames
    )
    {
        this.frozen = frozen;
        ActivitySources = activitySources;
        LoggedActivityNames = loggedActivityNames;
    }

    /// <summary>
    /// Creates an immutable copy of this options instance.
    /// </summary>
    /// <returns>The frozen options instance.</returns>
    public DiginsightActivitiesOptions Freeze()
    {
        if (frozen)
            return this;

        return new DiginsightActivitiesOptions(
            true,
            ActivitySources.ToFrozenDictionary(),
            LoggedActivityNames.ToFrozenDictionary()
        )
        {
            logBehavior = logBehavior,
            activityLogLevel = activityLogLevel,
            writeActivityActionAsPrefix = writeActivityActionAsPrefix,
            enablePayloadLogging = enablePayloadLogging,
            enablePayloadTagging = enablePayloadTagging,
            recordSpanDuration = recordSpanDuration,
            meterName = meterName,
            spanDurationMeterName = spanDurationMeterName,
            spanDurationMetricName = spanDurationMetricName,
            spanDurationMetricDescription = spanDurationMetricDescription,
        };
    }

    object IDynamicallyConfigurable.MakeFiller() => new Filler(this);

    object IVolatilelyConfigurable.MakeFiller() => new Filler(this);

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private class Filler
    {
#if NET || NETSTANDARD2_1_OR_GREATER
        private const char SpaceSeparator = ' ';
        private const char EqualsSeparator = '=';
#else
        private static readonly char[] SpaceSeparator = [ ' ' ];
        private static readonly char[] EqualsSeparator = [ '=' ];
#endif

        private readonly DiginsightActivitiesOptions filled;

        public LogBehavior LogBehavior
        {
            get => filled.LogBehavior;
            set => filled.LogBehavior = value;
        }

        public LogLevel ActivityLogLevel
        {
            get => filled.ActivityLogLevel;
            set => filled.ActivityLogLevel = value;
        }

        public bool EnablePayloadLogging
        {
            get => filled.EnablePayloadLogging;
            set => filled.EnablePayloadLogging = value;
        }

        public bool EnablePayloadTagging
        {
            get => filled.EnablePayloadTagging;
            set => filled.EnablePayloadTagging = value;
        }

        public string LoggedActivityNames
        {
            get => string.Join(" ", filled.LoggedActivityNames.Select(static kv => $"{kv.Key}={kv.Value:G}"));
            set
            {
                filled.LoggedActivityNames.Clear();
                filled.LoggedActivityNames.AddRange(
                    value.Split(SpaceSeparator, StringSplitOptions.RemoveEmptyEntries)
                        .Select(
                            static x => x.Split(EqualsSeparator, 2) switch
                            {
                                [ var x0 ] => KeyValuePair.Create(x0, LogBehavior.Show),
                                [ var x0, var x1 ] when Enum.TryParse(x1, true, out LogBehavior b) => KeyValuePair.Create(x0, b),
                                _ => (KeyValuePair<string, LogBehavior>?)null,
                            }
                        )
                        .OfType<KeyValuePair<string, LogBehavior>>()
                );
            }
        }

        public bool RecordSpanDuration
        {
            get => filled.RecordSpanDuration;
            set => filled.RecordSpanDuration = value;
        }

        public Filler(DiginsightActivitiesOptions filled)
        {
            this.filled = filled;
        }
    }
}
