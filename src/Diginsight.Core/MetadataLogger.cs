using Microsoft.Extensions.Logging;

namespace Diginsight;

public class MetadataLogger : MetadataLoggerBase
{
    public override ILogMetadata Metadata { get; }

    public MetadataLogger(ILogger decoratee, ILogMetadata metadata)
        : base(decoratee)
    {
        Metadata = metadata;
    }
}
