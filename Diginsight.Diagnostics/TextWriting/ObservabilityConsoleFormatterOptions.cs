using Microsoft.Extensions.Logging.Console;

namespace Diginsight.Diagnostics.TextWriting;

public sealed class ObservabilityConsoleFormatterOptions : ConsoleFormatterOptions, IObservabilityTextWritingOptions
{
    private readonly (int Width, LineDescriptor? Value)[]? descriptorCache;

    public IDictionary<string, string?> Patterns { get; } = new Dictionary<string, string?>();

    public ObservabilityConsoleFormatterOptions()
    {
        UseUtcTimestamp = true;
    }

    public LineDescriptor GetLineDescriptor(int? width)
    {
        throw new NotImplementedException();
    }
}
