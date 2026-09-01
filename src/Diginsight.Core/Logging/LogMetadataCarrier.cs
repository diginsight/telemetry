using System.ComponentModel;

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

    [EditorBrowsable(EditorBrowsableState.Never)]
    private void Deconstruct(out object? state, out ILogMetadata metadata)
    {
        state = State;
        metadata = Metadata;
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
    /// Enumerates metadata from the specified state, together with the residual state after each metadata extraction.
    /// </summary>
    /// <param name="state">The state from which to extract metadata.</param>
    /// <returns>A lazy enumerable of pairs containing the extracted metadata and the residual state.</returns>
    public static IEnumerable<(ILogMetadata Metadata, object? State)> EnumerateMetadata(object? state)
    {
        while (true)
        {
            if (state is not LogMetadataCarrier carrier)
            {
                yield break;
            }

            state = carrier.State;
            yield return (carrier.Metadata, state);
        }
    }

    /// <summary>
    /// Extracts metadata from the specified state.
    /// </summary>
    /// <param name="state">
    /// The state from which to extract metadata.
    /// Upon return, this parameter will contain the residual state after metadata extraction.
    /// </param>
    /// <param name="metadataCollection">The collection of extracted metadata.</param>
    public static void ExtractMetadata(ref object? state, out IEnumerable<ILogMetadata> metadataCollection)
    {
        ICollection<ILogMetadata> metadataList = [ ];
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

    /// <summary>
    /// Enumerates metadata of the desired type from the specified state, together with the residual state after each metadata extraction.
    /// </summary>
    /// <typeparam name="TMetadata">The type of the desired metadata.</typeparam>
    /// <param name="state">The state from which to extract metadata.</param>
    /// <returns>
    /// A lazy enumerable of pairs containing the extracted metadata and the residual state,
    /// preserving any metadata not matching <typeparamref name="TMetadata" />.
    /// </returns>
    public static IEnumerable<(TMetadata Metadata, object? State)> EnumerateMetadata<TMetadata>(object? state)
        where TMetadata : ILogMetadata
    {
        Stack<ILogMetadata> otherMetadataStack = [ ];

        while (true)
        {
            if (state is not LogMetadataCarrier carrier)
            {
                yield break;
            }

            (state, ILogMetadata metadata) = carrier;
            if (metadata is TMetadata desiredMetadata)
            {
                yield return (desiredMetadata, otherMetadataStack.Aggregate(state, For));
            }
            else
            {
                otherMetadataStack.Push(metadata);
            }
        }
    }

    /// <summary>
    /// Extracts metadata of the desired type from the specified state.
    /// </summary>
    /// <typeparam name="TMetadata">The type of the desired metadata.</typeparam>
    /// <param name="state">
    /// The state from which to extract metadata.
    /// Upon return, this parameter will contain the residual state after metadata extraction,
    /// preserving any metadata not matching <typeparamref name="TMetadata" />.
    /// </param>
    /// <param name="metadataCollection">The collection of extracted metadata.</param>
    public static void ExtractMetadata<TMetadata>(ref object? state, out IEnumerable<TMetadata> metadataCollection)
        where TMetadata : ILogMetadata
    {
        ICollection<TMetadata> metadataList = [ ];
        metadataCollection = metadataList;

        Stack<ILogMetadata> otherMetadataStack = [ ];

        while (true)
        {
            if (state is not LogMetadataCarrier carrier)
            {
                break;
            }

            (state, ILogMetadata metadata) = carrier;
            if (metadata is TMetadata desiredMetadata)
            {
                metadataList.Add(desiredMetadata);
            }
            else
            {
                otherMetadataStack.Push(metadata);
            }
        }

        state = otherMetadataStack.Aggregate(state, For);
    }

    /// <summary>
    /// Enumerates metadata matching the specified predicate from the specified state, together with the residual state after each metadata extraction.
    /// </summary>
    /// <param name="state">The state from which to extract metadata.</param>
    /// <param name="predicate">The predicate used to select the desired metadata.</param>
    /// <returns>
    /// A lazy enumerable of pairs containing the extracted metadata and the residual state,
    /// preserving any metadata not matching <paramref name="predicate" />.
    /// </returns>
    public static IEnumerable<(ILogMetadata Metadata, object? State)> EnumerateMetadata(object? state, Func<ILogMetadata, bool> predicate)
    {
        Stack<ILogMetadata> otherMetadataStack = [ ];

        while (true)
        {
            if (state is not LogMetadataCarrier carrier)
            {
                yield break;
            }

            (state, ILogMetadata metadata) = carrier;
            if (predicate(metadata))
            {
                yield return (metadata, otherMetadataStack.Aggregate(state, For));
            }
            else
            {
                otherMetadataStack.Push(metadata);
            }
        }
    }

    /// <summary>
    /// Extracts metadata matching the specified predicate from the specified state.
    /// </summary>
    /// <param name="state">
    /// The state from which to extract metadata.
    /// Upon return, this parameter will contain the residual state after metadata extraction,
    /// preserving any metadata not matching <paramref name="predicate" />.
    /// </param>
    /// <param name="predicate">The predicate used to select the desired metadata.</param>
    /// <param name="metadataCollection">The collection of extracted metadata.</param>
    public static void ExtractMetadata(ref object? state, Func<ILogMetadata, bool> predicate, out IEnumerable<ILogMetadata> metadataCollection)
    {
        ICollection<ILogMetadata> metadataList = [ ];
        metadataCollection = metadataList;

        Stack<ILogMetadata> otherMetadataStack = [ ];

        while (true)
        {
            if (state is not LogMetadataCarrier carrier)
            {
                break;
            }

            (state, ILogMetadata metadata) = carrier;
            if (predicate(metadata))
            {
                metadataList.Add(metadata);
            }
            else
            {
                otherMetadataStack.Push(metadata);
            }
        }

        state = otherMetadataStack.Aggregate(state, For);
    }
}
