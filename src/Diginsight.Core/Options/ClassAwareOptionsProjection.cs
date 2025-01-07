using Microsoft.Extensions.Options;

namespace Diginsight.Options;

/// <summary>
/// Wrapper for a class-aware options that projects it to a specific class.
/// </summary>
/// <typeparam name="TOptions">The type of options instance.</typeparam>
public sealed class ClassAwareOptionsProjection<TOptions> : IOptions<TOptions>
    where TOptions : class
{
    private readonly IClassAwareOptions<TOptions> underlying;
    private readonly Type @class;

    /// <inheritdoc />
    public TOptions Value => underlying.Get(@class);

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassAwareOptionsProjection{TOptions}" /> class.
    /// </summary>
    /// <param name="underlying">The underlying class-aware options.</param>
    /// <param name="class">The class type to be used.</param>
    public ClassAwareOptionsProjection(IClassAwareOptions<TOptions> underlying, Type @class)
    {
        this.underlying = underlying;
        this.@class = @class;
    }
}
