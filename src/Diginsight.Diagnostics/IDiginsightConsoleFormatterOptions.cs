namespace Diginsight.Diagnostics;

/// <summary>
/// Represents configuration options for the Diginsight console formatter.
/// </summary>
public interface IDiginsightConsoleFormatterOptions
{
    /// <summary>
    /// Gets the timestamp format.
    /// </summary>
    string? TimestampFormat { get; }
    /// <summary>
    /// Gets the time zone used to render timestamps.
    /// </summary>
    TimeZoneInfo? TimeZone { get; }

    /// <summary>
    /// Gets the console formatter pattern.
    /// </summary>
    /// <remarks>
    /// The pattern is parsed according to <see cref="Diginsight.Diagnostics.TextWriting.LineDescriptor" />. When <c>null</c>, the default line descriptor is used.
    /// </remarks>
    string? Pattern { get; }
    /// <summary>
    /// Gets the console formatter patterns keyed by minimum console width.
    /// </summary>
    /// <remarks>
    /// The patterns are parsed according to <see cref="Diginsight.Diagnostics.TextWriting.LineDescriptor" />. When <c>null</c>, the default line descriptor is used.
    /// </remarks>
    IReadOnlyDictionary<int, string?> Patterns { get; }

    /// <summary>
    /// Gets the total console line width.
    /// </summary>
    /// <remarks>
    /// A positive value is used as the fixed line width. A value of <c>0</c> auto-detects the current console window width. A negative value disables the width limit.
    /// </remarks>
    int TotalWidth { get; }

    /// <summary>
    /// Gets a value indicating whether console output uses colors.
    /// </summary>
    bool UseColor { get; }
}
