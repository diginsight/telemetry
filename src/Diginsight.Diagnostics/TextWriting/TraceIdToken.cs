using Pastel;
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

        public void Append(StringBuilder sb, ref int length, in LinePrefixData linePrefixData, bool useColor)
        {
            string traceId = ((linePrefixData.Activity?.TraceId)?.ToString() ?? "").PadLeft(32);
            sb.Append(useColor ? traceId.Pastel(ConsoleColor.White) : traceId);
            length += 32;
        }
    }
}
