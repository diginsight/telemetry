namespace Diginsight.SmartCache;

public interface IValueEntry
{
    object? Data { get; }
    Type Type { get; }
    DateTime CreationDate { get; }
}
