using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics;

public sealed class DiginsightOptions : IDiginsightOptions
{
    private LogLevel defaultActivityLogLevel = LogLevel.Debug;
    private bool logActivities;
    private bool recordActivities;
    private bool recordSpanDurations = true;

    private bool frozen;

    public LogLevel DefaultActivityLogLevel
    {
        get => defaultActivityLogLevel;
        set => defaultActivityLogLevel = frozen ? throw new InvalidOperationException("Options are frozen") : value;
    }

    public bool LogActivities
    {
        get => logActivities;
        set => logActivities = frozen ? throw new InvalidOperationException("Options are frozen") : value;
    }

    public ICollection<string> LoggedActivityNames { get; private set; } = new List<string>();

    IEnumerable<string> IDiginsightOptions.LoggedActivityNames => LoggedActivityNames;

    public ICollection<string> NonLoggedActivityNames { get; private set; } = new List<string>();

    IEnumerable<string> IDiginsightOptions.NonLoggedActivityNames => NonLoggedActivityNames;

    public bool RecordActivities
    {
        get => recordActivities;
        set => recordActivities = frozen ? throw new InvalidOperationException("Options are frozen") : value;
    }

    public ICollection<string> RecordedActivityNames { get; private set; } = new List<string>();

    IEnumerable<string> IDiginsightOptions.RecordedActivityNames => RecordedActivityNames;

    public ICollection<string> NonRecordedActivityNames { get; private set; } = new List<string>();

    IEnumerable<string> IDiginsightOptions.NonRecordedActivityNames => NonRecordedActivityNames;

    public bool RecordSpanDurations
    {
        get => recordSpanDurations;
        set => recordSpanDurations = frozen ? throw new InvalidOperationException("Options are frozen") : value;
    }

    public IDiginsightOptions Freeze()
    {
        if (frozen)
            return this;

        frozen = true;

        LoggedActivityNames = LoggedActivityNames.Distinct().ToArray();
        NonLoggedActivityNames = NonLoggedActivityNames.Distinct().ToArray();
        RecordedActivityNames = RecordedActivityNames.Distinct().ToArray();
        NonRecordedActivityNames = NonRecordedActivityNames.Distinct().ToArray();

        return this;
    }
}
