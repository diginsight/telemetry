using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

/// <summary>
/// Represents a line token that appends the elapsed time since the previous log entry to the line prefix.
/// </summary>
public sealed class DeltaToken : ILineToken
{
    /// <summary>
    /// Represents the singleton <see cref="DeltaToken" /> instance.
    /// </summary>
    public static readonly ILineToken Instance = new DeltaToken();

    private DeltaToken() { }

    /// <inheritdoc />
    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.Appenders.Add(Appender.Instance);
    }

    /// <inheritdoc />
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
                linePrefixData is { LastWasStart: true, Duration: not null }
                    ? null
                    : (linePrefixData.Timestamp - linePrefixData.PrevTimestamp)?.TotalMilliseconds,
                useColor
            );
        }
    }
}
