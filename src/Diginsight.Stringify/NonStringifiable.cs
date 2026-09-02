namespace Diginsight.Stringify;

/// <summary>
/// Represents a value that appends a marker for a type that must not be stringified.
/// </summary>
public sealed class NonStringifiable : IStringifiable
{
    private readonly Type type;

    bool IStringifiable.IsDeep => false;
    object? IStringifiable.Subject => null;

    /// <summary>
    /// Initializes a new instance of the <see cref="NonStringifiable" /> class.
    /// </summary>
    /// <param name="type">The type.</param>
    public NonStringifiable(Type type)
    {
        this.type = type;
    }

    /// <inheritdoc />
    public void AppendTo(StringifyContext stringifyContext)
    {
        stringifyContext
            .ComposeAndAppendType(type)
            .AppendDirect('!');
    }
}
