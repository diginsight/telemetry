using Diginsight.SmartCache.Externalization;
using Newtonsoft.Json;

namespace Diginsight.SmartCache;

public static class ValueEntry
{
    public static IValueEntry Create(object? data, Type type, DateTimeOffset creationDate)
    {
        return (IValueEntry)typeof(ValueEntry<>).MakeGenericType(type)
            .GetConstructor([ type, typeof(DateTimeOffset) ])!
            .Invoke([ data, creationDate ]);
    }
}

[CacheInterchangeName("VE")]
public sealed class ValueEntry<T> : IValueEntry
{
    public DateTimeOffset CreationDate { get; }
    public T Data { get; }
    public Type Type { get; } = typeof(T);

    object? IValueEntry.Data => Data;

    [JsonConstructor]
    public ValueEntry(T data, DateTimeOffset creationDate)
    {
        Data = data;
        CreationDate = creationDate;
    }
}
