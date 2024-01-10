using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

internal sealed class IndentationAppender
{
    private readonly int maxDepth;

    public IndentationAppender(int? maxDepth)
    {
        this.maxDepth = maxDepth ?? 10;
    }

    public void Append(StringBuilder sb, [NotNull] ref StrongBox<int>? depthBox, Activity? activity, bool isActivity, out int indentationLength)
    {
        DepthAppender.GetDepth(ref depthBox, activity);
        int depth = depthBox.Value;

        indentationLength = maxDepth < 0 || depth <= maxDepth
            ? depth * 2 - (isActivity ? 1 : 0)
            : maxDepth * 2;
        sb.Append(new string(' ', indentationLength));
    }
}
