using Microsoft.Extensions.Logging;
using System.Collections.Immutable;

namespace Diginsight.Diagnostics;

public sealed class DiginsightActivitiesOptions
    : IDiginsightActivitiesOptions, IDiginsightActivityNamesOptions
{
    private LogLevel defaultActivityLogLevel = LogLevel.Debug;
    private bool logActivities;
    private bool recordActivities;
    private bool recordSpanDurations;

    private ICollection<string>? loggedActivityNames;
    private ICollection<string>? nonLoggedActivityNames;
    private ICollection<string>? recordedActivityNames;
    private ICollection<string>? nonRecordedActivityNames;

    private readonly bool frozen;
    private bool writeActivityActionAsPrefix;

    public LogLevel DefaultActivityLogLevel
    {
        get => defaultActivityLogLevel;
        set => defaultActivityLogLevel = frozen ? throw new InvalidOperationException($"{nameof(DiginsightActivitiesOptions)} instance is frozen") : value;
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

    public ICollection<string> LoggedActivityNames => loggedActivityNames ??= new List<string>();

    IEnumerable<string> IDiginsightActivityNamesOptions.LoggedActivityNames => LoggedActivityNames;

    public ICollection<string> NonLoggedActivityNames => nonLoggedActivityNames ??= new List<string>();

    IEnumerable<string> IDiginsightActivityNamesOptions.NonLoggedActivityNames => NonLoggedActivityNames;

    public ICollection<string> RecordedActivityNames => recordedActivityNames ??= new List<string>();

    IEnumerable<string> IDiginsightActivityNamesOptions.RecordedActivityNames => RecordedActivityNames;

    public ICollection<string> NonRecordedActivityNames => nonRecordedActivityNames ??= new List<string>();

    IEnumerable<string> IDiginsightActivityNamesOptions.NonRecordedActivityNames => NonRecordedActivityNames;

    public DiginsightActivitiesOptions()
        : this(false) { }

    private DiginsightActivitiesOptions(bool frozen)
    {
        this.frozen = frozen;
    }

    public DiginsightActivitiesOptions Freeze()
    {
        return new (true)
        {
            defaultActivityLogLevel = DefaultActivityLogLevel,
            logActivities = LogActivities,
            recordActivities = RecordActivities,
            recordSpanDurations = RecordSpanDurations,
            loggedActivityNames = ImmutableArray.CreateRange(LoggedActivityNames.Distinct()),
            nonLoggedActivityNames = ImmutableArray.CreateRange(NonLoggedActivityNames.Distinct()),
            recordedActivityNames = ImmutableArray.CreateRange(RecordedActivityNames.Distinct()),
            nonRecordedActivityNames = ImmutableArray.CreateRange(NonRecordedActivityNames.Distinct()),
        };
    }
}
