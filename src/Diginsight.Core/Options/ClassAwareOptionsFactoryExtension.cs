using Microsoft.Extensions.Options;

namespace Diginsight.Options;

/// <summary>
/// Wrapper for an options factory that extends it to fake class-aware behavior.
/// </summary>
/// <typeparam name="TOptions">The type of options instance.</typeparam>
public sealed class ClassAwareOptionsFactoryExtension<TOptions> : IClassAwareOptionsFactory<TOptions>
    where TOptions : class
{
    private readonly IOptionsFactory<TOptions> underlying;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassAwareOptionsFactoryExtension{TOptions}" /> class.
    /// </summary>
    /// <param name="underlying">The underlying options factory.</param>
    public ClassAwareOptionsFactoryExtension(IOptionsFactory<TOptions> underlying)
    {
        this.underlying = underlying;
    }

    /// <inheritdoc />
    TOptions IOptionsFactory<TOptions>.Create(string name) => underlying.Create(name);

    /// <inheritdoc />
    public TOptions Create(string name, Type @class) => underlying.Create(name);
}
