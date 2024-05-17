using Diginsight.CAOptions;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Diagnostics;

public sealed class DiginsightActivitiesOptions
    : IDiginsightActivitiesOptions, IDiginsightActivityNamesOptions, IDynamicallyPostConfigurable
{
    private readonly bool frozen;

    private bool logActivities;
    private LogLevel activityLogLevel = LogLevel.Debug;
    private bool writeActivityActionAsPrefix;
    private bool disablePayloadRendering;
    private bool recordSpanDurations;

    public ICollection<string> ActivitySources { get; }

    IEnumerable<string> IDiginsightActivitiesOptions.ActivitySources => ActivitySources;

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

    public ICollection<string> LoggedActivityNames { get; }

    IEnumerable<string> IDiginsightActivityNamesOptions.LoggedActivityNames => LoggedActivityNames;

    public ICollection<string> NonLoggedActivityNames { get; }

    IEnumerable<string> IDiginsightActivityNamesOptions.NonLoggedActivityNames => NonLoggedActivityNames;

    public DiginsightActivitiesOptions()
        : this(
            false,
            new List<string>(),
            new List<string>(),
            new List<string>()
        ) { }

    private DiginsightActivitiesOptions(
        bool frozen,
        ICollection<string> activitySources,
        ICollection<string> loggedActivityNames,
        ICollection<string> nonLoggedActivityNames
    )
    {
        this.frozen = frozen;
        ActivitySources = activitySources;
        LoggedActivityNames = loggedActivityNames;
        NonLoggedActivityNames = nonLoggedActivityNames;
    }

    public DiginsightActivitiesOptions Freeze()
    {
        return new DiginsightActivitiesOptions(
            true,
            ImmutableArray.CreateRange(ActivitySources.Distinct()),
            ImmutableArray.CreateRange(LoggedActivityNames.Distinct()),
            ImmutableArray.CreateRange(NonLoggedActivityNames.Distinct())
        )
        {
            logActivities = LogActivities,
            activityLogLevel = ActivityLogLevel,
            writeActivityActionAsPrefix = WriteActivityActionAsPrefix,
            recordSpanDurations = RecordSpanDurations,
        };
    }

    object IDynamicallyPostConfigurable.MakeFiller() => new Filler(this);

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private class Filler
    {
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

        public Filler(DiginsightActivitiesOptions filled)
        {
            this.filled = filled;
        }
    }
}
