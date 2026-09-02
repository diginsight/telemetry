using Microsoft.Extensions.Logging.Console;
using System.Globalization;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents configuration options for the Diginsight console formatter.
/// </summary>
public sealed class DiginsightConsoleFormatterOptions : ConsoleFormatterOptions, IDiginsightConsoleFormatterOptions
{
    private TimeZoneInfo? timeZone;
    private bool useUtcTimestamp;

    /// <summary>
    /// Gets a value indicating whether timestamps are converted to UTC.
    /// </summary>
    /// <remarks>
    /// This property is obsolete; use <see cref="TimeZone" /> instead.
    /// </remarks>
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

    /// <summary>
    /// Gets the time zone used to render timestamps.
    /// </summary>
    public TimeZoneInfo? TimeZone
    {
        get => timeZone;
        set
        {
            timeZone = value;
            useUtcTimestamp = TimeZoneInfo.Utc.Equals(value);
        }
    }

    /// <summary>
    /// Gets the console formatter pattern.
    /// </summary>
    /// <remarks>
    /// The pattern is parsed according to <see cref="Diginsight.Diagnostics.TextWriting.LineDescriptor" />. When <c>null</c>, the default line descriptor is used.
    /// </remarks>
    public string? Pattern
    {
        get;
        set => field = value.HardTrim();
    }

    /// <summary>
    /// Gets the console formatter patterns keyed by minimum console width.
    /// </summary>
    /// <remarks>
    /// The patterns are parsed according to <see cref="Diginsight.Diagnostics.TextWriting.LineDescriptor" />. When <c>null</c>, the default line descriptor is used.
    /// </remarks>
    public IDictionary<string, string?> Patterns { get; } = new Dictionary<string, string?>();

    IReadOnlyDictionary<int, string?> IDiginsightConsoleFormatterOptions.Patterns =>
        new DictionaryView<string, string?, int, string?>(
            Patterns, static k => int.Parse(k, CultureInfo.InvariantCulture), static k => k.ToStringInvariant(), static v => v
        );

    /// <summary>
    /// Gets the total console line width.
    /// </summary>
    /// <remarks>
    /// A positive value is used as the fixed line width. A value of <c>0</c> auto-detects the current console window width. A negative value disables the width limit.
    /// </remarks>
    public int TotalWidth { get; set; }

    /// <summary>
    /// Gets a value indicating whether console output uses colors.
    /// </summary>
    public bool UseColor { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiginsightConsoleFormatterOptions" /> class with default configuration.
    /// </summary>
    public DiginsightConsoleFormatterOptions()
    {
        TimeZone = TimeZoneInfo.Utc;
    }
}
