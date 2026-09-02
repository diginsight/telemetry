using Microsoft.Extensions.Options;

namespace Diginsight.Options;

/// <summary>
/// Represents an interface for a class-aware snapshot of options that is recomputed for each request.
/// </summary>
/// <typeparam name="TOptions">The type of options being requested.</typeparam>
public interface IClassAwareOptionsSnapshot<out TOptions> : IClassAwareOptions<TOptions>, IOptionsSnapshot<TOptions>
    where TOptions : class
{
    /// <summary>
    /// Gets the configured options for the specified name and class.
    /// </summary>
    /// <param name="name">The name of the options, or <c>null</c> for the default name.</param>
    /// <param name="class">The class the options are resolved for, or <c>null</c> for no class.</param>
    /// <returns>The configured options instance.</returns>
    TOptions Get(string? name, Type? @class);

#if NET || NETSTANDARD2_1_OR_GREATER
    TOptions IClassAwareOptions<TOptions>.Get(Type? @class) => Get(null, @class);

    TOptions IOptionsSnapshot<TOptions>.Get(string? name) => Get(name, null);
#endif
}
