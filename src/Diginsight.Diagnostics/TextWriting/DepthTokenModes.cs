namespace Diginsight.Diagnostics.TextWriting;

/// <summary>
/// Represents depth components that can be appended by a <see cref="DepthToken" />.
/// </summary>
[Flags]
public enum DepthTokenModes
{
    /// <summary>
    /// Includes the activity layer depth.
    /// </summary>
    Layer = 1 << 0,

    /// <summary>
    /// Includes the local visual activity depth.
    /// </summary>
    Local = 1 << 1,

    /// <summary>
    /// Includes the cumulated visual activity depth.
    /// </summary>
    Cumulated = 1 << 2,
}
