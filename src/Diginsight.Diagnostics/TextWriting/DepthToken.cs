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
#if NET
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

        public void Append(StringBuilder sb, ref int length, in LinePrefixData linePrefixData, bool useColor)
        {
            int previousLength = sb.Length;

            ActivityDepth depth = linePrefixData.Activity.GetDepth();

            bool first = true;
            if ((modes & DepthTokenModes.Layer) != 0)
            {
                first = false;
#if NET
                sb.Append($"{depth.Layer,2}");
#else
                sb.AppendFormat("{0,1}", depth.Layer);
#endif
            }

            if ((modes & DepthTokenModes.Local) != 0)
            {
                if (!first)
                {
                    sb.Append('.');
                }
                first = false;

#if NET
                sb.Append($"{depth.VizLocal,2}");
#else
                sb.AppendFormat("{0,2}", depth.VizLocal);
#endif
            }

            if ((modes & DepthTokenModes.Cumulated) != 0)
            {
                if (!first)
                {
                    sb.Append('.');
                }

#if NET
                sb.Append($"{depth.VizCumulated,2}");
#else
                sb.AppendFormat("{0,2}", depth.VizCumulated);
#endif
            }

            length += sb.Length - previousLength;
        }
    }
}
