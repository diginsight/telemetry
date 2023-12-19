using Newtonsoft.Json;

namespace Diginsight.SmartCache;

public static class ValueEntry
{
    public static IValueEntry Create(object? data, Type type, DateTime? creationDate = null)
    {
        return (IValueEntry)typeof(ValueEntry<>).MakeGenericType(type)
            .GetConstructor([type, typeof(DateTime?)])!
            .Invoke([data, creationDate]);
    }
}

[CacheInterchangeName("VE")]
public sealed class ValueEntry<T> : IValueEntry
{
    public DateTime CreationDate { get; }
    public T Data { get; }
    public Type Type { get; } = typeof(T);

    object? IValueEntry.Data => Data;

    [JsonConstructor]
    public ValueEntry(T data, DateTime? creationDate = null)
    {
        Data = data;
        CreationDate = creationDate ?? DateTime.UtcNow;
    }
}
