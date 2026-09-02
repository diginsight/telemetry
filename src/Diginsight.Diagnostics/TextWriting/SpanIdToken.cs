using Pastel;
using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

/// <summary>
/// Represents a line token that appends the current activity span identifier to the line prefix.
/// </summary>
public sealed class SpanIdToken : ILineToken
{
    /// <summary>
    /// Represents the singleton <see cref="SpanIdToken" /> instance.
    /// </summary>
    public static readonly ILineToken Instance = new SpanIdToken();

    private SpanIdToken() { }

    /// <inheritdoc />
    public void Apply(ref MutableLineDescriptor lineDescriptor)
    {
        lineDescriptor.Appenders.Add(Appender.Instance);
    }

    /// <inheritdoc />
    public ILineToken Clone() => this;

    private sealed class Appender : IPrefixTokenAppender
    {
        public static readonly Appender Instance = new ();

        private Appender() { }

        public void Append(StringBuilder sb, ref int length, in LinePrefixData linePrefixData, bool useColor)
        {
            string spanId = ((linePrefixData.Activity?.SpanId)?.ToString() ?? "").PadLeft(16);
            sb.Append(useColor ? spanId.Pastel(ConsoleColor.White) : spanId);
            length += 16;
        }
    }
}
