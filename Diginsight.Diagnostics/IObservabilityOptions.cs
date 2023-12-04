using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics;

public interface IObservabilityOptions
{
    LogLevel DefaultActivityLogLevel { get; }

    bool RecordActivities { get; }
}
