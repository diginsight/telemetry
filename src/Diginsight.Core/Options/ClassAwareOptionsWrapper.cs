using System.Collections.Frozen;

namespace Diginsight.Options;

/// <summary>
/// <see cref="IClassAwareOptions{TOptions}" /> wrapper that returns the options instances passed in the constructor.
/// </summary>
/// <typeparam name="TOptions">The type of options being requested.</typeparam>
public sealed class ClassAwareOptionsWrapper<TOptions> : IClassAwareOptions<TOptions>
    where TOptions : class
{
    private readonly IReadOnlyDictionary<Type, TOptions> valuesByClass;

    /// <summary>
    /// Gets the no-class options instance.
    /// </summary>
    public TOptions Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassAwareOptionsWrapper{TOptions}" /> class.
    /// </summary>
    /// <param name="noClassValue">The no-class options instance.</param>
    /// <param name="valuesByClass">A dictionary of options instances by class.</param>
    public ClassAwareOptionsWrapper(TOptions noClassValue, IReadOnlyDictionary<Type, TOptions>? valuesByClass = null)
    {
        this.valuesByClass = valuesByClass ?? FrozenDictionary<Type, TOptions>.Empty;
        Value = noClassValue;
    }

    /// <summary>
    /// Gets the options instance associated with the specified class.
    /// </summary>
    /// <param name="class">The class to get the options for, or <c>null</c> for <see cref="ClassAwareOptions.NoClass" />.</param>
    /// <returns>The options instance.</returns>
    public TOptions Get(Type? @class)
    {
        @class ??= ClassAwareOptions.NoClass;
        return @class != ClassAwareOptions.NoClass && valuesByClass.TryGetValue(@class, out TOptions? value) ? value : Value;
    }
}
