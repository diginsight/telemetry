namespace Diginsight.Diagnostics.TextWriting;

/// <summary>
/// Represents a line token that contributes to a text-writing line descriptor.
/// </summary>
public interface ILineToken
{
    /// <summary>
    /// Applies the token to the specified mutable line descriptor.
    /// </summary>
    /// <param name="lineDescriptor">The mutable line descriptor to update.</param>
    void Apply(ref MutableLineDescriptor lineDescriptor);

    /// <summary>
    /// Creates a copy of the line token.
    /// </summary>
    /// <returns>The copied line token.</returns>
    ILineToken Clone();
}
