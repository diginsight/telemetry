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
    /// <param name="configuration">The configuration to filter.</param>
    extension(IConfiguration configuration)
    {
        /// <summary>
        /// Filters the configuration by the specified class.
        /// </summary>
        /// <param name="class">The class to filter by.</param>
        /// <returns>A filtered configuration.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IConfiguration FilterBy(Type? @class) => FilteredConfiguration.For(configuration, @class);

        /// <summary>
        /// Filters the configuration by <see cref="ClassAwareOptions.NoClass" />.
        /// </summary>
        /// <returns>A filtered configuration.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IConfiguration FilterByNoClass() => FilteredConfiguration.ForNoClass(configuration);
    }
}
