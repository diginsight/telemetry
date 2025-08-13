using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics;

public interface IDiginsightActivitiesLogOptions
{
    LogBehavior LogBehavior { get; }
    LogLevel ActivityLogLevel { get; }
    bool WriteActivityActionAsPrefix { get; }
    bool DisablePayloadRendering { get; }
}
