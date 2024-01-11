using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

internal sealed class DurationAppender : MsecAppender
{
    public static readonly DurationAppender Instance = new ();

    private DurationAppender() { }

    public override void Append(StringBuilder sb, in LinePrefixData linePrefixData) => Append(sb, linePrefixData.Duration);
}
