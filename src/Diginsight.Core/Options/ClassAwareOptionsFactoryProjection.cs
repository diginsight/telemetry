using Microsoft.Extensions.Options;

namespace Diginsight.Options;

/// <summary>
/// Wrapper for a class-aware options factory that projects it to a specific class.
/// </summary>
/// <typeparam name="TOptions">The type of options instance.</typeparam>
public sealed class ClassAwareOptionsFactoryProjection<TOptions> : IOptionsFactory<TOptions>
    where TOptions : class
{
    private readonly IClassAwareOptionsFactory<TOptions> underlying;
    private readonly Type @class;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassAwareOptionsFactoryProjection{TOptions}" /> class.
    /// </summary>
    /// <param name="underlying">The underlying class-aware options factory.</param>
    /// <param name="class">The class type to be used.</param>
    public ClassAwareOptionsFactoryProjection(IClassAwareOptionsFactory<TOptions> underlying, Type @class)
    {
        this.underlying = underlying;
        this.@class = @class;
    }

    /// <inheritdoc />
    public TOptions Create(string name) => underlying.Create(name, @class);
}
