using Microsoft.Extensions.Logging.Console;

namespace Diginsight.Diagnostics;

public sealed class DiginsightConsoleFormatterOptions : ConsoleFormatterOptions, IDiginsightConsoleFormatterOptions
{
    private string? pattern;
    private readonly Dictionary<string, string?> patterns = new ();

    public string? Pattern
    {
        get => pattern;
        set => pattern = value.HardTrim();
    }

    public IDictionary<string, string?> Patterns => patterns;

    IReadOnlyDictionary<string, string?> IDiginsightConsoleFormatterOptions.Patterns => patterns;

    public DiginsightConsoleFormatterOptions()
    {
        UseUtcTimestamp = true;
    }
}
