using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

/// <summary>
/// Represents a line token that appends the activity duration to the line prefix.
/// </summary>
public sealed class DurationToken : ILineToken
{
    /// <summary>
    /// Represents the singleton <see cref="DurationToken" /> instance.
    /// </summary>
    public static readonly ILineToken Instance = new DurationToken();

    private DurationToken() { }

    /// <inheritdoc />
    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.Appenders.Add(Appender.Instance);
    }

    /// <inheritdoc />
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
