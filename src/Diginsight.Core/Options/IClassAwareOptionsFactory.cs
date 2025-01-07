using Microsoft.Extensions.Options;

namespace Diginsight.Options;

/// <summary>
/// Used to create class-aware <typeparamref name="TOptions" /> instances.
/// </summary>
/// <typeparam name="TOptions">The type of options being requested.</typeparam>
public interface IClassAwareOptionsFactory<TOptions> : IOptionsFactory<TOptions>
    where TOptions : class
{
    /// <summary>
    /// Creates an instance of <typeparamref name="TOptions" /> for the specified name and class.
    /// </summary>
    /// <param name="name">The name of the options instance.</param>
    /// <param name="class">The class type associated with the options instance.</param>
    /// <returns>An instance of <typeparamref name="TOptions" />.</returns>
    TOptions Create(string name, Type @class);

#if NET || NETSTANDARD2_1_OR_GREATER
    TOptions IOptionsFactory<TOptions>.Create(string name) => Create(name, ClassAwareOptions.NoClass);
#endif
}
