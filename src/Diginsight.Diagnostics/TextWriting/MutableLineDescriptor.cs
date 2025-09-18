using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Diagnostics.TextWriting;

public ref struct MutableLineDescriptor
{
    [SuppressMessage("ReSharper", "ReplaceWithFieldKeyword")]
    private ICollection<IPrefixTokenAppender>? appenders;

    public ICollection<IPrefixTokenAppender> Appenders => appenders ??= new List<IPrefixTokenAppender>();

    public int? MaxIndentedDepth { get; set; }

    public int? MaxMessageLength { get; set; }

    public int? MaxLineLength { get; set; }

    public MutableLineDescriptor(LineDescriptor descriptor)
    {
        Appenders.AddRange(descriptor.Appenders);
        MaxIndentedDepth = descriptor.MaxIndentedDepth;
        MaxMessageLength = descriptor.MaxMessageLength;
        MaxLineLength = descriptor.MaxLineLength;
    }
}
