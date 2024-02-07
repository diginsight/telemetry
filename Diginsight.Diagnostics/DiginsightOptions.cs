using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics;

public sealed class DiginsightOptions : IDiginsightOptions
{
    public LogLevel DefaultActivityLogLevel { get; set; } = LogLevel.Debug;

    public bool RecordActivities { get; set; }

    public bool RecordSpanDurations { get; set; } = true;

    public ICollection<string> RecordedActivityNames { get; } = new List<string>();

    public ICollection<string> NotRecordedActivityNames { get; } = new List<string>();

    IEnumerable<string> IDiginsightOptions.RecordedActivityNames => RecordedActivityNames;

    IEnumerable<string> IDiginsightOptions.NotRecordedActivityNames => NotRecordedActivityNames;
}
