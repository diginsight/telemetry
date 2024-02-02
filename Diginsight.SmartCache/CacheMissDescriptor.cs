using Newtonsoft.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Diginsight.SmartCache;

public sealed class CacheMissDescriptor
{
    private readonly object? value;

    public string Emitter { get; }

    public ICacheKey Key { get; }

    public DateTime Timestamp { get; }

    public string Location { get; }

    public Type? ValueType { get; }

    public object? Value => HasValue ? value : throw new InvalidOperationException("descriptor contains no value");

    [JsonIgnore]
    private bool HasValue => ValueType is not null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CacheMissDescriptor(
        string emitter,
        ICacheKey key,
        DateTime timestamp,
        string location,
        (Type Type, object? Value)? valueTuple
    )
        : this(emitter, key, timestamp, location, valueTuple?.Type, valueTuple?.Value) { }

    [JsonConstructor]
    private CacheMissDescriptor(
        string emitter,
        ICacheKey key,
        DateTime timestamp,
        string location,
        Type? valueType,
        object? value
    )
    {
        Emitter = emitter;
        Key = key;
        Timestamp = timestamp;
        Location = location;
        ValueType = valueType;
        this.value = value;
    }

    public void Deconstruct(out string emitter, out ICacheKey key, out DateTime timestamp, out string location, out Type? valueType)
    {
        emitter = Emitter;
        key = Key;
        timestamp = Timestamp;
        location = Location;
        valueType = ValueType;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool ShouldSerializeValue() => HasValue;
}
