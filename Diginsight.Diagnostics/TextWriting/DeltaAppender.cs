using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

internal sealed class DeltaAppender : MsecAppender
{
    public static readonly DeltaAppender Instance = new ();

    private DeltaAppender() { }

    public override void Append(StringBuilder sb, LinePrefixData linePrefixData)
    {
        Append(sb, linePrefixData.LastWasStart ? null : (linePrefixData.Timestamp - linePrefixData.PrevTimestamp)?.TotalMilliseconds);
    }
}
