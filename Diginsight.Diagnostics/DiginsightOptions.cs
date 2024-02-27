using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics;

public sealed class DiginsightOptions : IDiginsightOptions
{
    private LogLevel defaultActivityLogLevel = LogLevel.Debug;
    private bool recordActivities;
    private bool recordSpanDurations = true;

    private bool frozen;

    public LogLevel DefaultActivityLogLevel
    {
        get => defaultActivityLogLevel;
        set => defaultActivityLogLevel = frozen ? throw new InvalidOperationException("Options are frozen") : value;
    }

    public bool RecordActivities
    {
        get => recordActivities;
        set => recordActivities = frozen ? throw new InvalidOperationException("Options are frozen") : value;
    }

    public bool RecordSpanDurations
    {
        get => recordSpanDurations;
        set => recordSpanDurations = frozen ? throw new InvalidOperationException("Options are frozen") : value;
    }

    public ICollection<string> RecordedActivityNames { get; private set; } = new List<string>();

    IEnumerable<string> IDiginsightOptions.RecordedActivityNames => RecordedActivityNames;

    public ICollection<string> NotRecordedActivityNames { get; private set; } = new List<string>();

    IEnumerable<string> IDiginsightOptions.NotRecordedActivityNames => NotRecordedActivityNames;

    public IDiginsightOptions Freeze()
    {
        if (frozen)
            return this;

        frozen = true;

        RecordedActivityNames = RecordedActivityNames.Distinct().ToArray();
        NotRecordedActivityNames = NotRecordedActivityNames.Distinct().ToArray();

        return this;
    }
}
