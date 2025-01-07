using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Diginsight.Options;

/// <summary>
/// Provides extension methods for <see cref="IConfiguration" /> objects.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ConfigurationExtensions
{
    /// <summary>
    /// Filters the configuration by the specified class.
    /// </summary>
    /// <param name="configuration">The configuration to filter.</param>
    /// <param name="class">The class to filter by.</param>
    /// <returns>A filtered configuration.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IConfiguration FilterBy(this IConfiguration configuration, Type? @class) =>
        FilteredConfiguration.For(configuration, @class);

    /// <summary>
    /// Filters the configuration by <see cref="ClassAwareOptions.NoClass" />.
    /// </summary>
    /// <param name="configuration">The configuration to filter.</param>
    /// <returns>A filtered configuration.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IConfiguration FilterByNoClass(this IConfiguration configuration) =>
        FilteredConfiguration.ForNoClass(configuration);
}
