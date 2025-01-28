using Microsoft.Extensions.Options;

namespace Diginsight.Options;

/// <summary>
/// Wrapper for an options snapshot in order to fake class-aware behavior, or for class-aware options in order to fake snapshot behavior.
/// </summary>
/// <typeparam name="TOptions">The type of options instance.</typeparam>
public sealed class ClassAwareOptionsSnapshotExtension<TOptions> : IClassAwareOptionsSnapshot<TOptions>
    where TOptions : class
{
    private readonly IOptionsSnapshot<TOptions>? underlying1;
    private readonly IClassAwareOptions<TOptions>? underlying2;

    /// <inheritdoc />
    public TOptions Value => ((IOptions<TOptions>?)underlying1 ?? underlying2)!.Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassAwareOptionsSnapshotExtension{TOptions}" /> class.
    /// </summary>
    /// <param name="underlying">The underlying options snapshot.</param>
    public ClassAwareOptionsSnapshotExtension(IOptionsSnapshot<TOptions> underlying)
    {
        underlying1 = underlying;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassAwareOptionsSnapshotExtension{TOptions}" /> class.
    /// </summary>
    /// <param name="underlying">The underlying class-aware options.</param>
    public ClassAwareOptionsSnapshotExtension(IClassAwareOptions<TOptions> underlying)
    {
        underlying2 = underlying;
    }

    /// <inheritdoc />
    public TOptions Get(string? name) => underlying1 is { } underlying ? underlying.Get(name) : underlying2!.Get(null);

    /// <inheritdoc />
    public TOptions Get(Type? @class) => underlying1 is { } underlying ? underlying.Value : underlying2!.Get(@class);

    /// <inheritdoc />
    public TOptions Get(string? name, Type? @class) => underlying1 is { } underlying ? underlying.Get(name) : underlying2!.Get(@class);
}
