namespace Diginsight.Stringify;

/// <summary>
/// Represents the registration of a stringifier type with an ordering priority.
/// </summary>
public sealed class StringifierRegistration
{
    /// <summary>
    /// Gets the stringifier type.
    /// </summary>
    public Type Type { get; }
    /// <summary>
    /// Gets the registration priority.
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringifierRegistration" /> class.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="priority">The registration priority.</param>
    /// <exception cref="ArgumentNullException">Thrown when the type is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the type is not assignable to <see cref="IStringifier" />.</exception>
    public StringifierRegistration(Type type, int priority)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        if (!typeof(IStringifier).IsAssignableFrom(type))
            throw new ArgumentOutOfRangeException(nameof(type), $"Should be assignable to {nameof(IStringifier)}");

        Type = type;
        Priority = priority;
    }
}
