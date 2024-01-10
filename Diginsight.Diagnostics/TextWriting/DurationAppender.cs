using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

internal sealed class DurationAppender : MsecAppender
{
    public static readonly DurationAppender Instance = new ();

    private DurationAppender() { }

    public new void Append(StringBuilder sb, double? duration) => MsecAppender.Append(sb, duration);
}
