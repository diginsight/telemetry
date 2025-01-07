#if !(NET || NETSTANDARD2_1_OR_GREATER)
using Microsoft.Extensions.Options;
#endif

namespace Diginsight.Options;

/// <summary>
/// Default implementation of the <see cref="IClassAwareOptionsSnapshot{TOptions}" /> interface.
/// </summary>
/// <typeparam name="TOptions">The type of options to cache.</typeparam>
public sealed class ClassAwareOptionsManager<TOptions> : IClassAwareOptionsSnapshot<TOptions>
    where TOptions : class
{
    private readonly IClassAwareOptionsFactory<TOptions> factory;
    private readonly IClassAwareOptionsCache<TOptions> cache = new ClassAwareOptionsCache<TOptions>();

#if !(NET || NETSTANDARD2_1_OR_GREATER)
    TOptions IOptions<TOptions>.Value => Get(null, null);
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassAwareOptionsManager{TOptions}" /> class.
    /// </summary>
    /// <param name="factory">The factory to create option instances.</param>
    public ClassAwareOptionsManager(IClassAwareOptionsFactory<TOptions> factory)
    {
        this.factory = factory;
    }

    /// <inheritdoc />
    public TOptions Get(string? name, Type? @class)
    {
        name ??= Microsoft.Extensions.Options.Options.DefaultName;
        @class ??= ClassAwareOptions.NoClass;

        return cache.TryGetValue(name, @class, out TOptions? options)
            ? options
            : cache.GetOrAdd(name, @class, static (n, c, f) => f.Create(n, c), factory);
    }

#if !(NET || NETSTANDARD2_1_OR_GREATER)
    TOptions IClassAwareOptions<TOptions>.Get(Type? @class) => Get(null, @class);

    TOptions IOptionsSnapshot<TOptions>.Get(string? name) => Get(name, null);
#endif
}
