using System.Diagnostics;
using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

internal sealed class TraceIdAppender : IPrefixTokenAppender
{
    public static readonly TraceIdAppender Instance = new ();

    private TraceIdAppender() { }

    public void Append(StringBuilder sb, ActivityTraceId? traceId)
    {
        sb.Append((traceId?.ToString() ?? "").PadLeft(32));
    }
}
