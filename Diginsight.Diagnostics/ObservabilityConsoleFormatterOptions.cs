using Microsoft.Extensions.Logging.Console;

namespace Diginsight.Diagnostics;

public sealed class ObservabilityConsoleFormatterOptions : ConsoleFormatterOptions, IObservabilityConsoleFormatterOptions
{
    private string? pattern;
    private readonly Dictionary<string, string?> patterns = new ();

    public string? Pattern
    {
        get => pattern;
        set => pattern = value.HardTrim();
    }

    public IDictionary<string, string?> Patterns => patterns;

    IReadOnlyDictionary<string, string?> IObservabilityConsoleFormatterOptions.Patterns => patterns;

    public ObservabilityConsoleFormatterOptions()
    {
        UseUtcTimestamp = true;
    }
}
