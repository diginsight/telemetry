using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics;

public interface IDiginsightOptions
{
    LogLevel DefaultActivityLogLevel { get; }

    bool RecordActivities { get; }

    bool RecordSpanDurations { get; }

    IEnumerable<string> RecordedActivityNames { get; }

    IEnumerable<string> NotRecordedActivityNames { get; }
}
