using Microsoft.Extensions.Logging.Console;
using System.Globalization;

namespace Diginsight.Diagnostics;

public sealed class DiginsightConsoleFormatterOptions : ConsoleFormatterOptions, IDiginsightConsoleFormatterOptions
{
    private string? pattern;

    public string? Pattern
    {
        get => pattern;
        set => pattern = value.HardTrim();
    }

    public IDictionary<string, string?> Patterns { get; } = new Dictionary<string, string?>();

    IReadOnlyDictionary<int, string?> IDiginsightConsoleFormatterOptions.Patterns =>
        new DictionaryView<string, string?, int, string?>(
            Patterns, static k => int.Parse(k, CultureInfo.InvariantCulture), static k => k.ToStringInvariant(), static v => v
        );

    public int TotalWidth { get; set; }

    public bool UseColor { get; set; }

    public DiginsightConsoleFormatterOptions()
    {
        UseUtcTimestamp = true;
    }
}
