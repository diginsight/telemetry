using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics;

public interface IDiginsightActivitiesLogOptions
{
    bool LogActivities { get; }
    LogLevel ActivityLogLevel { get; }
    bool WriteActivityActionAsPrefix { get; }
    bool DisablePayloadRendering { get; }
}
