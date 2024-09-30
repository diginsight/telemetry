using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

public sealed class DurationToken : ILineToken
{
    public static readonly ILineToken Instance = new DurationToken();

    private DurationToken() { }

    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.Appenders.Add(Appender.Instance);
    }

    public ILineToken Clone() => this;

    private sealed class Appender : MsecAppender
    {
        public static readonly Appender Instance = new ();

        private Appender() { }

        public override void Append(StringBuilder sb, ref int length, in LinePrefixData linePrefixData, bool useColor)
        {
            Append(sb, ref length, linePrefixData.Duration, useColor);
        }
    }
}
