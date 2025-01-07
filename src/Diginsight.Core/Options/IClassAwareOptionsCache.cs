using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Options;

/// <summary>
/// Provides a cache for class-aware <typeparamref name="TOptions" /> instances.
/// </summary>
/// <typeparam name="TOptions">The type of options being cached.</typeparam>
public interface IClassAwareOptionsCache<TOptions>
    where TOptions : class
{
    /// <summary>
    /// Gets or adds an instance of <typeparamref name="TOptions" /> for the specified name and class.
    /// </summary>
    /// <param name="name">The name of the options instance.</param>
    /// <param name="class">The class associated with the options instance.</param>
    /// <param name="create">The function to create the options instance if it does not exist.</param>
    /// <returns>An instance of <typeparamref name="TOptions" />.</returns>
    TOptions GetOrAdd(string name, Type @class, Func<string, Type, TOptions> create);

    /// <summary>
    /// Gets or adds an instance of <typeparamref name="TOptions" /> for the specified name, class, and additional argument.
    /// </summary>
    /// <typeparam name="TArg">The type of the additional argument.</typeparam>
    /// <param name="name">The name of the options instance.</param>
    /// <param name="class">The class associated with the options instance.</param>
    /// <param name="create">The function to create the options instance if it does not exist.</param>
    /// <param name="creatorArg">The additional argument for the creation function.</param>
    /// <returns>An instance of <typeparamref name="TOptions" />.</returns>
    TOptions GetOrAdd<TArg>(string name, Type @class, Func<string, Type, TArg, TOptions> create, TArg creatorArg);

    /// <summary>
    /// Tries to get an instance of <typeparamref name="TOptions" /> for the specified name and class.
    /// </summary>
    /// <param name="name">The name of the options instance.</param>
    /// <param name="class">The class associated with the options instance.</param>
    /// <param name="options">When this method returns <c>true</c>, contains the options instance if found; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the options instance was found; otherwise, <c>false</c>.</returns>
    bool TryGetValue(string name, Type @class, [NotNullWhen(true)] out TOptions? options);

    /// <summary>
    /// Tries to add an instance of <typeparamref name="TOptions" /> for the specified name and class.
    /// </summary>
    /// <param name="name">The name of the options instance.</param>
    /// <param name="class">The class associated with the options instance.</param>
    /// <param name="options">The options instance to add.</param>
    /// <returns><c>true</c> if the options instance was added; otherwise, <c>false</c>.</returns>
    bool TryAdd(string name, Type @class, TOptions options);

    /// <summary>
    /// Tries to remove an instance of <typeparamref name="TOptions" /> for the specified name and class.
    /// </summary>
    /// <param name="name">The name of the options instance.</param>
    /// <param name="class">The class associated with the options instance.</param>
    /// <returns><c>true</c> if the options instance was removed; otherwise, <c>false</c>.</returns>
    bool TryRemove(string name, Type @class);

    /// <summary>
    /// Tries to remove all instances of <typeparamref name="TOptions" /> for the specified name.
    /// </summary>
    /// <param name="name">The name of the options instances.</param>
    /// <returns>A collection of classes that were removed.</returns>
    IEnumerable<Type> TryRemove(string name);

    /// <summary>
    /// Clears all cached options instances.
    /// </summary>
    /// <returns>A collection of tuples containing the names and associated classes of the cleared options instances.</returns>
    IEnumerable<(string Name, IEnumerable<Type> Classes)> Clear();
}
