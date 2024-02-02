namespace Diginsight.SmartCache;

public readonly record struct CacheLocationOutput<TValue>(TValue Item, long ValueSerializedSize, double LatencyMsec);
