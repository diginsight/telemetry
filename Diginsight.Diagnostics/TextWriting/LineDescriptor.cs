namespace Diginsight.Diagnostics.TextWriting;

public ref struct LineDescriptor
{
    private ICollection<IPrefixTokenAppender>? customAppenders;

    public ICollection<IPrefixTokenAppender> CustomAppenders => customAppenders ??= new List<IPrefixTokenAppender>();

    public int? MaxIndentedDepth { get; set; }

    public int? MaxMessageLength { get; set; }

    public int? MaxLineLength { get; set; }
}
