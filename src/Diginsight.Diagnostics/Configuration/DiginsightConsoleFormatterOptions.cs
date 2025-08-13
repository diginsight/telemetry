using Microsoft.Extensions.Logging.Console;
using System.Globalization;

namespace Diginsight.Diagnostics;

public sealed class DiginsightConsoleFormatterOptions : ConsoleFormatterOptions, IDiginsightConsoleFormatterOptions
{
    private TimeZoneInfo? timeZone;
    private bool useUtcTimestamp;
    private string? pattern;

    [Obsolete($"This property hides the one in {nameof(ConsoleFormatterOptions)} and is not used by {nameof(DiginsightConsoleFormatter)}. Get/set {nameof(TimeZone)} instead.")]
    public new bool UseUtcTimestamp
    {
        get => useUtcTimestamp;
        set
        {
            useUtcTimestamp = value;
            timeZone = value ? TimeZoneInfo.Utc : null;
        }
    }

    public TimeZoneInfo? TimeZone
    {
        get => timeZone;
        set
        {
            timeZone = value;
            useUtcTimestamp = TimeZoneInfo.Utc.Equals(value);
        }
    }

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
        TimeZone = TimeZoneInfo.Utc;
    }
}
