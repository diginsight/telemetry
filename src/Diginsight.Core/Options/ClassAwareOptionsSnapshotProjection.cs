using Microsoft.Extensions.Options;

namespace Diginsight.Options;

/// <summary>
/// Wrapper for a class-aware options snapshot that projects it to a specific class.
/// </summary>
/// <typeparam name="TOptions">The type of options instance.</typeparam>
public sealed class ClassAwareOptionsSnapshotProjection<TOptions> : IOptionsSnapshot<TOptions>
    where TOptions : class
{
    private readonly IClassAwareOptionsSnapshot<TOptions> underlying;
    private readonly Type @class;

    /// <inheritdoc />
    public TOptions Value => Get(null);

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassAwareOptionsSnapshotProjection{TOptions}" /> class.
    /// </summary>
    /// <param name="underlying">The underlying class-aware options snapshot.</param>
    /// <param name="class">The class type to be used.</param>
    public ClassAwareOptionsSnapshotProjection(IClassAwareOptionsSnapshot<TOptions> underlying, Type @class)
    {
        this.underlying = underlying;
        this.@class = @class;
    }

    /// <inheritdoc />
    public TOptions Get(string? name) => underlying.Get(name, @class);
}
