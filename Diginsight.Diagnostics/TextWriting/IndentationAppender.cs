using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

internal sealed class IndentationAppender
{
    private readonly int maxIndentedDepth;

    public IndentationAppender(int? maxIndentedDepth)
    {
        this.maxIndentedDepth = maxIndentedDepth ?? 10;
    }

    public void Append(StringBuilder sb, [NotNull] ref StrongBox<int>? depthBox, Activity? activity, bool isActivity, out int indentationLength)
    {
        DepthAppender.GetDepth(ref depthBox, activity);
        int depth = depthBox.Value;

        indentationLength = maxIndentedDepth < 0 || depth <= maxIndentedDepth
            ? depth * 2 - (isActivity ? 1 : 0)
            : maxIndentedDepth * 2;
        sb.Append(new string(' ', indentationLength));
    }
}
