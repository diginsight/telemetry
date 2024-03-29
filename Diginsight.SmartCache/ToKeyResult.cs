﻿namespace Diginsight.SmartCache;

public readonly struct ToKeyResult
{
    public static readonly ToKeyResult None = default;

    public bool Success => UntypedKey is not null;

    public ICacheKey? Key { get; }

    public object? UntypedKey { get; }

    public ToKeyResult(object untypedKey)
    {
        UntypedKey = untypedKey ?? throw new ArgumentNullException(nameof(untypedKey));
        Key = untypedKey as ICacheKey;
    }

    public ToKeyResult(ICacheKey key)
        : this((object)key) { }
}
