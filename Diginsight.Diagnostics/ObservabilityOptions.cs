using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics;

public sealed class ObservabilityOptions : IObservabilityOptions
{
    public LogLevel DefaultActivityLogLevel { get; set; } = LogLevel.Debug;

    public bool RecordActivities { get; set; }

    public ICollection<string> RecordedActivityNames { get; } = new List<string>();

    public ICollection<string> NotRecordedActivityNames { get; } = new List<string>();

    IEnumerable<string> IObservabilityOptions.RecordedActivityNames => RecordedActivityNames;

    IEnumerable<string> IObservabilityOptions.NotRecordedActivityNames => NotRecordedActivityNames;
}
