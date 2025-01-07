using Microsoft.Extensions.Options;

namespace Diginsight.Options;

/// <summary>
/// Represents the settings for Diginsight's implementation of <see cref="IOptionsMonitorCache{TOptions}" />.
/// </summary>
public sealed class OptionsCacheSettings
{
    /// <summary>
    /// Gets the set of options entries which are dynamic (i.e. non cachable), identified by option type and option name.
    /// </summary>
    /// <remarks>
    /// A <c>null</c> value for the second tuple component indicates that all options of the specified type are dynamic.
    /// </remarks>
    public ISet<(Type, string?)> DynamicEntries { get; } = new HashSet<(Type, string?)>(new TupleEqualityComparer<Type, string?>(c2: StringComparer.Ordinal));
}
