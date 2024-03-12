using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics;

public interface IDiginsightOptions
{
    LogLevel DefaultActivityLogLevel { get; }

    bool LogActivities { get; }

    bool RecordActivities { get; }

    bool RecordSpanDurations { get; }
}
