using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics;

public interface IDiginsightActivitiesOptions
{
    bool LogActivities { get; }
    LogLevel ActivityLogLevel { get; }
    bool WriteActivityActionAsPrefix { get; }

    bool RecordSpanDurations { get; }
}
