namespace Diginsight.Logging;

public class LogMetadataCarrier
{
    public object? State { get; }
    public ILogMetadata Metadata { get; }

    internal LogMetadataCarrier(object? state, ILogMetadata metadata)
    {
        State = state;
        Metadata = metadata;
    }

    public static LogMetadataCarrier For(object? state, ILogMetadata metadata)
    {
        return state is IEnumerable<KeyValuePair<string, object?>> tags
            ? new TaggedLogMetadataCarrier(state, metadata, tags)
            : new LogMetadataCarrier(state, metadata);
    }

    public static (LogMetadataCarrier State, Func<LogMetadataCarrier, Exception?, string> Formatter) For<T>(
        T state, ILogMetadata metadata, Func<T, Exception?, string> formatter
    )
    {
        return (For(state, metadata), (s, e) => formatter((T)s.State!, e));
    }

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
