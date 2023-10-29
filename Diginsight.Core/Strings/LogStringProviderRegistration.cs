namespace Diginsight.Strings;

public sealed class LogStringProviderRegistration
{
    public Type Type { get; }
    public int Priority { get; }

    public LogStringProviderRegistration(Type type, int priority)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        if (!typeof(ILogStringProvider).IsAssignableFrom(type))
            throw new ArgumentOutOfRangeException(nameof(type), $"should be assignable to {nameof(ILogStringProvider)}");

        Type = type;
        Priority = priority;
    }
}
