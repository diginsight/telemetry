using Diginsight.CAOptions;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Diagnostics;

public sealed class DiginsightActivitiesOptions
    : IDiginsightActivitiesOptions, IDiginsightActivityNamesOptions, IDynamicallyPostConfigurable
{
    private readonly bool frozen;

    private LogLevel activityLogLevel = LogLevel.Debug;
    private bool logActivities;
    private bool recordActivities;
    private bool writeActivityActionAsPrefix;
    private bool recordSpanDurations;

    public LogLevel ActivityLogLevel
    {
        get => activityLogLevel;
        set => activityLogLevel = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    public bool LogActivities
    {
        get => logActivities;
        set => logActivities = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    public bool RecordActivities
    {
        get => recordActivities;
        set => recordActivities = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
    }

    public bool WriteActivityActionAsPrefix
    {
        get => writeActivityActionAsPrefix;
        set => writeActivityActionAsPrefix = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
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

    public ICollection<string> RecordedActivityNames { get; }

    IEnumerable<string> IDiginsightActivityNamesOptions.RecordedActivityNames => RecordedActivityNames;

    public ICollection<string> NonRecordedActivityNames { get; }

    IEnumerable<string> IDiginsightActivityNamesOptions.NonRecordedActivityNames => NonRecordedActivityNames;

    public DiginsightActivitiesOptions()
        : this(
            false,
            new List<string>(),
            new List<string>(),
            new List<string>(),
            new List<string>()
        ) { }

    private DiginsightActivitiesOptions(
        bool frozen,
        ICollection<string> loggedActivityNames,
        ICollection<string> nonLoggedActivityNames,
        ICollection<string> recordedActivityNames,
        ICollection<string> nonRecordedActivityNames
    )
    {
        this.frozen = frozen;
        LoggedActivityNames = loggedActivityNames;
        NonLoggedActivityNames = nonLoggedActivityNames;
        RecordedActivityNames = recordedActivityNames;
        NonRecordedActivityNames = nonRecordedActivityNames;
    }

    public DiginsightActivitiesOptions Freeze()
    {
        return new (
            true,
            ImmutableArray.CreateRange(LoggedActivityNames.Distinct()),
            ImmutableArray.CreateRange(NonLoggedActivityNames.Distinct()),
            ImmutableArray.CreateRange(RecordedActivityNames.Distinct()),
            ImmutableArray.CreateRange(NonRecordedActivityNames.Distinct())
        )
        {
            activityLogLevel = ActivityLogLevel,
            logActivities = LogActivities,
            recordActivities = RecordActivities,
            writeActivityActionAsPrefix = WriteActivityActionAsPrefix,
            recordSpanDurations = RecordSpanDurations,
        };
    }

    object IDynamicallyPostConfigurable.MakeFiller() => new Filler(this);

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private class Filler
    {
        private readonly DiginsightActivitiesOptions filled;

        public LogLevel ActivityLogLevel
        {
            get => filled.ActivityLogLevel;
            set => filled.ActivityLogLevel = value;
        }

        public bool LogActivities
        {
            get => filled.LogActivities;
            set => filled.LogActivities = value;
        }

        public bool RecordActivities
        {
            get => filled.RecordActivities;
            set => filled.RecordActivities = value;
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
