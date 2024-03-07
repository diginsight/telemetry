using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics;

public interface IDiginsightOptions
{
    LogLevel DefaultActivityLogLevel { get; }

    bool LogActivities { get; }
    IEnumerable<string> LoggedActivityNames { get; }
    IEnumerable<string> NonLoggedActivityNames { get; }

    bool RecordActivities { get; }
    IEnumerable<string> RecordedActivityNames { get; }
    IEnumerable<string> NonRecordedActivityNames { get; }

    bool RecordSpanDurations { get; }
}
