using Diginsight.Options;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Diagnostics;

public sealed class DiginsightActivitiesOptions
    : IDiginsightActivitiesOptions,
        IDiginsightActivityNamesOptions,
        IDiginsightActivitiesLogOptions,
        IDiginsightActivitiesMetricOptions,
        IDynamicallyConfigurable,
        IVolatilelyConfigurable
{
    private readonly bool frozen;

    private bool logActivities;
    private LogLevel activityLogLevel = LogLevel.Debug;
    private bool writeActivityActionAsPrefix;
    private bool disablePayloadRendering;
    private bool recordSpanDurations;
    private string? meterName;
    private string? metricName;
    private string? metricUnit;
    private string? metricDescription;

    public ICollection<string> ActivitySources { get; }

    IEnumerable<string> IDiginsightActivitiesOptions.ActivitySources => ActivitySources;

    public ICollection<string> NotActivitySources { get; }

    IEnumerable<string> IDiginsightActivitiesOptions.NotActivitySources => NotActivitySources;

    public bool LogActivities
    {
        get => logActivities;
        set => logActivities = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    public LogLevel ActivityLogLevel
    {
        get => activityLogLevel;
        set => activityLogLevel = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    public bool WriteActivityActionAsPrefix
    {
        get => writeActivityActionAsPrefix;
        set => writeActivityActionAsPrefix = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    public bool DisablePayloadRendering
    {
        get => disablePayloadRendering;
        set => disablePayloadRendering = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    public bool RecordSpanDurations
    {
        get => recordSpanDurations;
        set => recordSpanDurations = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    public string? MeterName
    {
        get => meterName;
        set => meterName = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    string IDiginsightActivitiesMetricOptions.MeterName => MeterName ?? throw new InvalidOperationException($"{nameof(MeterName)} is unset");

    public string MetricName
    {
        get => metricName ??= "diginsight.span_duration";
        set => metricName = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    public string? MetricUnit
    {
        get => metricUnit;
        set => metricUnit = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    public string? MetricDescription
    {
        get => metricDescription;
        set => metricDescription = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    public ICollection<string> LoggedActivityNames { get; }

    IEnumerable<string> IDiginsightActivityNamesOptions.LoggedActivityNames => LoggedActivityNames;

    public ICollection<string> NonLoggedActivityNames { get; }

    IEnumerable<string> IDiginsightActivityNamesOptions.NonLoggedActivityNames => NonLoggedActivityNames;

    public ICollection<string> SpanMeasuredActivityNames { get; }

    IEnumerable<string> IDiginsightActivityNamesOptions.SpanMeasuredActivityNames => SpanMeasuredActivityNames;

    public ICollection<string> NonSpanMeasuredActivityNames { get; }

    IEnumerable<string> IDiginsightActivityNamesOptions.NonSpanMeasuredActivityNames => NonSpanMeasuredActivityNames;

    public DiginsightActivitiesOptions()
        : this(
            false,
            new List<string>(),
            new List<string>(),
            new List<string>(),
            new List<string>(),
            new List<string>(),
            new List<string>()
        ) { }

    private DiginsightActivitiesOptions(
        bool frozen,
        ICollection<string> activitySources,
        ICollection<string> notActivitySources,
        ICollection<string> loggedActivityNames,
        ICollection<string> nonLoggedActivityNames,
        ICollection<string> spanMeasuredActivityNames,
        ICollection<string> nonSpanMeasuredActivityNames
    )
    {
        this.frozen = frozen;
        ActivitySources = activitySources;
        NotActivitySources = notActivitySources;
        LoggedActivityNames = loggedActivityNames;
        NonLoggedActivityNames = nonLoggedActivityNames;
        SpanMeasuredActivityNames = spanMeasuredActivityNames;
        NonSpanMeasuredActivityNames = nonSpanMeasuredActivityNames;
    }

    public DiginsightActivitiesOptions Freeze()
    {
        return new DiginsightActivitiesOptions(
            true,
            ImmutableArray.CreateRange(ActivitySources.Distinct()),
            ImmutableArray.CreateRange(NotActivitySources.Distinct()),
            ImmutableArray.CreateRange(LoggedActivityNames.Distinct()),
            ImmutableArray.CreateRange(NonLoggedActivityNames.Distinct()),
            ImmutableArray.CreateRange(SpanMeasuredActivityNames.Distinct()),
            ImmutableArray.CreateRange(NonSpanMeasuredActivityNames.Distinct())
        )
        {
            logActivities = logActivities,
            activityLogLevel = activityLogLevel,
            writeActivityActionAsPrefix = writeActivityActionAsPrefix,
            recordSpanDurations = recordSpanDurations,
            meterName = meterName,
            metricName = metricName,
            metricUnit = metricUnit,
            metricDescription = metricDescription,
        };
    }

    object IDynamicallyConfigurable.MakeFiller() => new Filler(this);

    object IVolatilelyConfigurable.MakeFiller() => new Filler(this);

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private class Filler
    {
#if NET || NETSTANDARD2_1_OR_GREATER
        private const char SpaceSeparator = ' ';
#else
        private static readonly char[] SpaceSeparator = [ ' ' ];
#endif

        private readonly DiginsightActivitiesOptions filled;

        public bool LogActivities
        {
            get => filled.LogActivities;
            set => filled.LogActivities = value;
        }

        public LogLevel ActivityLogLevel
        {
            get => filled.ActivityLogLevel;
            set => filled.ActivityLogLevel = value;
        }

        public bool DisablePayloadRendering
        {
            get => filled.DisablePayloadRendering;
            set => filled.DisablePayloadRendering = value;
        }

        public bool RecordSpanDurations
        {
            get => filled.RecordSpanDurations;
            set => filled.RecordSpanDurations = value;
        }

        public string LoggedActivityNames
        {
            get => string.Join(" ", filled.LoggedActivityNames);
            set
            {
                filled.LoggedActivityNames.Clear();
                filled.LoggedActivityNames.AddRange(value.Split(SpaceSeparator, StringSplitOptions.RemoveEmptyEntries));
            }
        }

        public string NonLoggedActivityNames
        {
            get => string.Join(" ", filled.NonLoggedActivityNames);
            set
            {
                filled.NonLoggedActivityNames.Clear();
                filled.NonLoggedActivityNames.AddRange(value.Split(SpaceSeparator, StringSplitOptions.RemoveEmptyEntries));
            }
        }

        public string SpanMeasuredActivityNames
        {
            get => string.Join(" ", filled.SpanMeasuredActivityNames);
            set
            {
                filled.SpanMeasuredActivityNames.Clear();
                filled.SpanMeasuredActivityNames.AddRange(value.Split(SpaceSeparator, StringSplitOptions.RemoveEmptyEntries));
            }
        }

        public string NonSpanMeasuredActivityNames
        {
            get => string.Join(" ", filled.NonSpanMeasuredActivityNames);
            set
            {
                filled.NonSpanMeasuredActivityNames.Clear();
                filled.NonSpanMeasuredActivityNames.AddRange(value.Split(SpaceSeparator, StringSplitOptions.RemoveEmptyEntries));
            }
        }

        public Filler(DiginsightActivitiesOptions filled)
        {
            this.filled = filled;
        }
    }
}
