using System.Text;

namespace Diginsight.Diagnostics.TextWriting;

/// <summary>
/// Represents an interface for appending a line-prefix token.
/// </summary>
public interface IPrefixTokenAppender
{
    /// <summary>
    /// Appends a prefix token to the specified string builder.
    /// </summary>
    /// <param name="sb">The string builder to append to.</param>
    /// <param name="length">The current visible prefix length.</param>
    /// <param name="linePrefixData">The data used to build the line prefix.</param>
    /// <param name="useColor">Whether to emit ANSI color sequences.</param>
    void Append(StringBuilder sb, ref int length, in LinePrefixData linePrefixData, bool useColor);
}
