using Microsoft.Extensions.Options;

namespace Diginsight.Options;

/// <summary>
/// Used to retrieve class-aware configured <typeparamref name="TOptions" /> instances.
/// </summary>
/// <typeparam name="TOptions">The type of options being requested.</typeparam>
public interface IClassAwareOptions<out TOptions> : IOptions<TOptions>
    where TOptions : class
{
#if NET || NETSTANDARD2_1_OR_GREATER
    TOptions IOptions<TOptions>.Value => Get(null);
#endif

    /// <summary>
    /// Gets the options instance associated with the specified class.
    /// </summary>
    /// <param name="class">The class type to get the options for.</param>
    /// <returns>The options instance.</returns>
    TOptions Get(Type? @class);
}
