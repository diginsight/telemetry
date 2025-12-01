using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics;

public interface IDiginsightActivitiesLogOptions
{
    IReadOnlyDictionary<string, LogBehavior> ActivityNames { get; }
    LogBehavior LogBehavior { get; }
    LogLevel LogLevel { get; }
    bool WriteActivityActionAsPrefix { get; }
    bool DisablePayloadRendering { get; }
}
