using Microsoft.Extensions.Options;

namespace Diginsight.Options;

/// <summary>
/// Wrapper for options that extends it to fake class-aware behavior.
/// </summary>
/// <typeparam name="TOptions">The type of options instance.</typeparam>
public sealed class ClassAwareOptionsExtension<TOptions> : IClassAwareOptions<TOptions>
    where TOptions : class
{
    private readonly IOptions<TOptions> underlying;

    /// <inheritdoc />
    public TOptions Value => underlying.Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassAwareOptionsExtension{TOptions}" /> class.
    /// </summary>
    /// <param name="underlying">The underlying options.</param>
    public ClassAwareOptionsExtension(IOptions<TOptions> underlying)
    {
        this.underlying = underlying;
    }

    /// <inheritdoc />
    public TOptions Get(Type? @class) => underlying.Value;
}
