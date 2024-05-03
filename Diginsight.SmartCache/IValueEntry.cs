namespace Diginsight.SmartCache;

public interface IValueEntry
{
    object? Data { get; }
    Type Type { get; }
    DateTimeOffset CreationDate { get; }
}
