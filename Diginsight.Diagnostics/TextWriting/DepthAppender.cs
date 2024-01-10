using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

internal sealed class DepthAppender : IPrefixTokenAppender
{
    public static readonly DepthAppender Instance = new ();

    private DepthAppender() { }

    public void Append(StringBuilder sb, [NotNull] ref StrongBox<int>? depthBox, Activity? activity)
    {
        GetDepth(ref depthBox, activity);

#if NET6_0_OR_GREATER
        sb.Append($"{depthBox.Value,2}");
#else
        sb.AppendFormat("{0,2}", depthBox.Value);
#endif
    }

    public static void GetDepth([NotNull] ref StrongBox<int>? depthBox, Activity? activity)
    {
        if (depthBox is not null)
        {
            return;
        }

        int depth = 0;
        for (; activity is not null; activity = activity.Parent)
        {
            depth++;
        }

        depthBox = new (depth);
    }
}
