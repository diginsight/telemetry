using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

public sealed class DepthToken : ILineToken
{
    public DepthTokenModes Modes { get; set; }

    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.Appenders.Add(new Appender(Modes));
    }

    public ILineToken Clone() => new DepthToken() { Modes = Modes };

    private sealed class Appender : IPrefixTokenAppender
    {
        private static readonly DepthTokenModes ModesMask =
#if NET6_0_OR_GREATER
            Enum.GetValues<DepthTokenModes>()
#else
            Enum.GetValues(typeof(DepthTokenModes)).Cast<DepthTokenModes>()
#endif
                .Aggregate(static (x, a) => x | a);

        private readonly DepthTokenModes modes;

        public Appender(DepthTokenModes modes)
        {
            modes &= ModesMask;
            this.modes = modes != 0 ? modes : DepthTokenModes.Local;
        }

        public void Append(StringBuilder sb, in LinePrefixData linePrefixData)
        {
            bool first = true;
            if ((modes & DepthTokenModes.Layer) != 0)
            {
                first = false;
#if NET6_0_OR_GREATER
                sb.Append($"{linePrefixData.Depth.Layer,2}");
#else
                sb.AppendFormat("{0,1}", linePrefixData.Depth.Layer);
#endif
            }

            if ((modes & DepthTokenModes.Local) != 0)
            {
                if (!first)
                {
                    sb.Append('.');
                }
                first = false;

#if NET6_0_OR_GREATER
                sb.Append($"{linePrefixData.Depth.Local,2}");
#else
                sb.AppendFormat("{0,2}", linePrefixData.Depth.Local);
#endif
            }

            if ((modes & DepthTokenModes.Cumulated) != 0)
            {
                if (!first)
                {
                    sb.Append('.');
                }

#if NET6_0_OR_GREATER
                sb.Append($"{linePrefixData.Depth.Cumulated,2}");
#else
                sb.AppendFormat("{0,2}", linePrefixData.Depth.Cumulated);
#endif
            }
        }
    }
}