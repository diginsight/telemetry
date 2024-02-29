namespace Diginsight.SmartCache.Externalization;

public readonly record struct CacheLocationOutput<TValue>(TValue Item, long ValueSerializedSize, double LatencyMsec);
