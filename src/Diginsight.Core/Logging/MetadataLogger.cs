using Microsoft.Extensions.Logging;

namespace Diginsight.Logging;

/// <summary>
/// Represents a logger that includes metadata.
/// </summary>
public class MetadataLogger : MetadataLoggerBase
{
    /// <inheritdoc />
    public override ILogMetadata Metadata { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataLogger" /> class.
    /// </summary>
    /// <param name="decoratee">The underlying logger to decorate.</param>
    /// <param name="metadata">The metadata to associate with the logger.</param>
    public MetadataLogger(ILogger decoratee, ILogMetadata metadata)
        : base(decoratee)
    {
        Metadata = metadata;
    }
}
