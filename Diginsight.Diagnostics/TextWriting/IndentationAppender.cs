using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

internal sealed class IndentationAppender
{
    private readonly int maxDepth;

    public IndentationAppender(int? maxDepth)
    {
        this.maxDepth = maxDepth ?? 10;
    }

    public void Append(StringBuilder sb, LinePrefixData linePrefixData, out int indentationLength)
    {
        int depth = linePrefixData.Depth;
        indentationLength = maxDepth < 0 || depth <= maxDepth
            ? depth * 2 - (linePrefixData.IsActivity ? 1 : 0)
            : maxDepth * 2;
        sb.Append(new string(' ', indentationLength));
    }
}
