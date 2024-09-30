using System.Collections;

namespace Diginsight.Logging;

internal sealed class TaggedLogMetadataCarrier : LogMetadataCarrier, IEnumerable<KeyValuePair<string, object?>>
{
    private readonly IEnumerable<KeyValuePair<string, object?>> tags;

    public TaggedLogMetadataCarrier(object? state, ILogMetadata metadata, IEnumerable<KeyValuePair<string, object?>> tags)
        : base(state, metadata)
    {
        this.tags = tags;
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => tags.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
