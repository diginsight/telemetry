namespace Diginsight.Diagnostics.TextWriting;

/// <summary>
/// Represents an interface for parsing text-writing line tokens.
/// </summary>
public interface ILineTokenParser
{
    /// <summary>
    /// Gets the token name recognized by the parser.
    /// </summary>
    string TokenName { get; }

    /// <summary>
    /// Parses the specified token detail span.
    /// </summary>
    /// <param name="tokenDetailSpan">The token detail span to parse.</param>
    /// <returns>The parsed line token.</returns>
    /// <exception cref="FormatException">Thrown when the token detail span has an invalid format.</exception>
    ILineToken Parse(ReadOnlySpan<char> tokenDetailSpan);
}
