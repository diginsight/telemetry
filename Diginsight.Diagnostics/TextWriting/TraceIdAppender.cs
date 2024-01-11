using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

internal sealed class TraceIdAppender : IPrefixTokenAppender
{
    public static readonly TraceIdAppender Instance = new ();

    private TraceIdAppender() { }

    public void Append(StringBuilder sb, in LinePrefixData linePrefixData)
    {
        sb.Append((linePrefixData.TraceId?.ToString() ?? "").PadLeft(32));
    }
}
