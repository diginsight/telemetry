using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

internal sealed class DepthAppender : IPrefixTokenAppender
{
    public static readonly DepthAppender Instance = new ();

    private DepthAppender() { }

    public void Append(StringBuilder sb, LinePrefixData linePrefixData)
    {
#if NET6_0_OR_GREATER
        sb.Append($"{linePrefixData.Depth,2}");
#else
        sb.AppendFormat("{0,2}", linePrefixData.Depth);
#endif
    }
}
