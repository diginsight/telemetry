namespace Diginsight.Logging;

/// <summary>
/// Represents a carrier for log metadata.
/// </summary>
public class LogMetadataCarrier
{
    /// <summary>
    /// Gets the state associated with the log.
    /// </summary>
    public object? State { get; }

    /// <summary>
    /// Gets the metadata associated with the log.
    /// </summary>
    public ILogMetadata Metadata { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogMetadataCarrier" /> class.
    /// </summary>
    /// <param name="state">The state associated with the log.</param>
    /// <param name="metadata">The metadata associated with the log.</param>
    internal LogMetadataCarrier(object? state, ILogMetadata metadata)
    {
        State = state;
        Metadata = metadata;
    }

    /// <summary>
    /// Creates a new <see cref="LogMetadataCarrier" /> instance for the specified state and metadata.
    /// </summary>
    /// <param name="state">The state associated with the log.</param>
    /// <param name="metadata">The metadata associated with the log.</param>
    /// <returns>A new <see cref="LogMetadataCarrier" /> instance.</returns>
    public static LogMetadataCarrier For(object? state, ILogMetadata metadata)
    {
        return state is IEnumerable<KeyValuePair<string, object?>> tags
            ? new TaggedLogMetadataCarrier(state, metadata, tags)
            : new LogMetadataCarrier(state, metadata);
    }

    /// <summary>
    /// Creates a new <see cref="LogMetadataCarrier" /> instance for the specified state, metadata, and formatter.
    /// </summary>
    /// <typeparam name="T">The type of the state.</typeparam>
    /// <param name="state">The state associated with the log.</param>
    /// <param name="metadata">The metadata associated with the log.</param>
    /// <param name="formatter">The formatter function.</param>
    /// <returns>A tuple containing the <see cref="LogMetadataCarrier" /> instance and the formatter function.</returns>
    public static (LogMetadataCarrier State, Func<LogMetadataCarrier, Exception?, string> Formatter) For<T>(
        T state, ILogMetadata metadata, Func<T, Exception?, string> formatter
    )
    {
        return (For(state, metadata), (s, e) => formatter((T)s.State!, e));
    }

    /// <summary>
    /// Extracts metadata from the specified state.
    /// </summary>
    /// <param name="state">The state from which to extract metadata.</param>
    /// <param name="metadataCollection">The collection of extracted metadata.</param>
    public static void ExtractMetadata(ref object? state, out IEnumerable<ILogMetadata> metadataCollection)
    {
        ICollection<ILogMetadata> metadataList = new List<ILogMetadata>();
        metadataCollection = metadataList;

        while (true)
        {
            if (state is not LogMetadataCarrier carrier)
            {
                break;
            }

            state = carrier.State;
            metadataList.Add(carrier.Metadata);
        }
    }
}
