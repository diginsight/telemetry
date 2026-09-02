namespace Diginsight.Stringify;

/// <summary>
/// Represents an interface for converting objects into stringifiable representations.
/// </summary>
public interface IStringifier
{
    /// <summary>
    /// Attempts to convert an object into a stringifiable representation.
    /// </summary>
    /// <param name="obj">The object to stringify.</param>
    /// <returns>The stringifiable representation if the object is handled; otherwise, <c>null</c>.</returns>
    IStringifiable? TryStringify(object obj);
}
