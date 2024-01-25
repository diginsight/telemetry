namespace Diginsight.Diagnostics.TextWriting;

public ref struct MutableLineDescriptor
{
    private ICollection<IPrefixTokenAppender>? appenders;

    public ICollection<IPrefixTokenAppender> Appenders => appenders ??= new List<IPrefixTokenAppender>();

    public int? MaxIndentedDepth { get; set; }

    public int? MaxMessageLength { get; set; }

    public int? MaxLineLength { get; set; }
}
