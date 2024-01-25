using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

public sealed class TraceIdToken : ILineToken
{
    public static readonly ILineToken Instance = new TraceIdToken();

    private TraceIdToken() { }

    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.Appenders.Add(Appender.Instance);
    }

    public ILineToken Clone() => this;

    private sealed class Appender : IPrefixTokenAppender
    {
        public static readonly Appender Instance = new ();

        private Appender() { }

        public void Append(StringBuilder sb, in LinePrefixData linePrefixData)
        {
            sb.Append((linePrefixData.TraceId?.ToString() ?? "").PadLeft(32));
        }
    }
}
