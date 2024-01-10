using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

internal sealed class DeltaAppender : MsecAppender
{
    public static readonly DeltaAppender Instance = new ();

    private DeltaAppender() { }

    public void Append(StringBuilder sb, bool lastWasStart, DateTime timestamp, DateTime? prevTimestamp)
    {
        Append(sb, lastWasStart ? null : (timestamp - prevTimestamp)?.TotalMilliseconds);
    }
}
