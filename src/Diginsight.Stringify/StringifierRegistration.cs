namespace Diginsight.Stringify;

public sealed class StringifierRegistration
{
    public Type Type { get; }
    public int Priority { get; }

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
