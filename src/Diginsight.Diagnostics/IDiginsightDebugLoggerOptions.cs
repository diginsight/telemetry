namespace Diginsight.Diagnostics;

/// <summary>
/// Represents configuration options for the Diginsight debug logger.
/// </summary>
public interface IDiginsightDebugLoggerOptions
{
    /// <summary>
    /// Gets the debug logger pattern.
    /// </summary>
    /// <remarks>
    /// The pattern is parsed according to <see cref="Diginsight.Diagnostics.TextWriting.LineDescriptor" />. When <c>null</c>, the default line descriptor is used.
    /// </remarks>
    string? Pattern { get; }
    /// <summary>
    /// Gets the time zone used to render timestamps.
    /// </summary>
    TimeZoneInfo? TimeZone { get; }
}
