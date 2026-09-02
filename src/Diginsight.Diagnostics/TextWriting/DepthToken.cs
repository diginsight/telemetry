using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

/// <summary>
/// Represents a line token that appends activity depth information to the line prefix.
/// </summary>
public sealed class DepthToken : ILineToken
{
    /// <summary>
    /// Gets the activity depth modes to append.
    /// </summary>
    public DepthTokenModes Modes { get; set; }

    /// <inheritdoc />
    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.Appenders.Add(new Appender(Modes));
    }

    /// <inheritdoc />
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
                sb.Append($"{depth.VisualLocal,2}");
#else
                sb.AppendFormat("{0,2}", depth.VisualLocal);
#endif
            }

            if ((modes & DepthTokenModes.Cumulated) != 0)
            {
                if (!first)
                {
                    sb.Append('.');
                }

#if NET
                sb.Append($"{depth.VisualCumulated,2}");
#else
                sb.AppendFormat("{0,2}", depth.VisualCumulated);
#endif
            }

            length += sb.Length - previousLength;
        }
    }
}
