using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Diagnostics.TextWriting;

public ref struct MutableLineDescriptor
{
    [field: MaybeNull]
    public ICollection<IPrefixTokenAppender> Appenders => field ??= new List<IPrefixTokenAppender>();

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
