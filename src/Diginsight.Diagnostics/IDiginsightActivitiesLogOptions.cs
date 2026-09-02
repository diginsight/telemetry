using Microsoft.Extensions.Logging;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents activity lifecycle logging options.
/// </summary>
public interface IDiginsightActivitiesLogOptions
{
    /// <summary>
    /// Gets the activity name patterns mapped to activity lifecycle logging behavior.
    /// </summary>
    IReadOnlyDictionary<string, LogBehavior> ActivityNames { get; }
    /// <summary>
    /// Gets the default activity lifecycle logging behavior.
    /// </summary>
    LogBehavior LogBehavior { get; }
    /// <summary>
    /// Gets the activity lifecycle log level.
    /// </summary>
    LogLevel LogLevel { get; }
    /// <summary>
    /// Gets a value indicating whether activity lifecycle log actions are written before the activity name.
    /// </summary>
    bool WriteActivityActionAsPrefix { get; }
    /// <summary>
    /// Gets a value indicating whether activity input and output payloads are written to lifecycle logs.
    /// </summary>
    bool EnablePayloadLogging { get; }
    /// <summary>
    /// Gets a value indicating whether activity input and output payloads are added as activity tags.
    /// </summary>
    bool EnablePayloadTagging { get; }
}
