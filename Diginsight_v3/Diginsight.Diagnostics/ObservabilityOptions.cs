using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics;

public sealed class ObservabilityOptions : IObservabilityOptions
{
    public LogLevel DefaultActivityLogLevel { get; set; } = LogLevel.Debug;
}
