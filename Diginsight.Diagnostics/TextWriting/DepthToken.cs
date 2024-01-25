using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

public sealed class DepthToken : ILineToken
{
    public static readonly ILineToken Instance = new DepthToken();

    private DepthToken() { }

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
#if NET6_0_OR_GREATER
            sb.Append($"{linePrefixData.Depth,2}");
#else
        sb.AppendFormat("{0,2}", linePrefixData.Depth);
#endif
        }
    }
}
