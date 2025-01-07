using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

public sealed class DeltaToken : ILineToken
{
    public static readonly ILineToken Instance = new DeltaToken();

    private DeltaToken() { }

    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.Appenders.Add(Appender.Instance);
    }

    public ILineToken Clone() => this;

    internal sealed class Appender : MsecAppender
    {
        public static readonly Appender Instance = new ();

        private Appender() { }

        public override void Append(StringBuilder sb, ref int length, in LinePrefixData linePrefixData, bool useColor)
        {
            Append(
                sb,
                ref length,
                linePrefixData.LastWasStart ? null : (linePrefixData.Timestamp - linePrefixData.PrevTimestamp)?.TotalMilliseconds,
                useColor
            );
        }
    }
}
