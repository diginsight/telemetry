using Microsoft.Extensions.Configuration;

namespace Diginsight.Options;

/// <summary>
/// Represents a view of an <see cref="IConfiguration" />, filtered by an associated class.
/// </summary>
/// <remarks>
///     <para>
///     The filtered configuration system works by tagging a section key with a suffix.
///     The suffix is a <c>@</c> character, followed by a class marker, as per <see cref="ClassConfigurationMarkers" />.
///     </para>
///     <para>
///     A tagged configuration section (and its children) can be associated with any type that matches the class marker.
///     </para>
/// </remarks>
public interface IFilteredConfiguration : IConfiguration
{
    /// <summary>
    /// Gets the class associated with this configuration view.
    /// </summary>
    Type Class { get; }
}
