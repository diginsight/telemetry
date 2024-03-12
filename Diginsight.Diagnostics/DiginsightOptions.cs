using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics;

public sealed class DiginsightOptions : IDiginsightOptions
{
    public LogLevel DefaultActivityLogLevel { get; set; } = LogLevel.Debug;

    public bool LogActivities { get; set; }

    public bool RecordActivities { get; set; }

    public bool RecordSpanDurations { get; set; }
}
