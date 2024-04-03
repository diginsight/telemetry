using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics;

public interface IDiginsightActivitiesOptions
{
    LogLevel ActivityLogLevel { get; }

    bool LogActivities { get; }
    bool RecordActivities { get; }
    bool WriteActivityActionAsPrefix { get; }

    bool RecordSpanDurations { get; }
}
