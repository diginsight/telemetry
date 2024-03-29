﻿using Microsoft.Extensions.Logging;

namespace Diginsight.SmartCache.Externalization;

public class CachePayloadHolder<T>
    where T : notnull
{
    private readonly ILogger logger;
    private readonly KeyValuePair<string, object?> metricTag;
    private readonly object lockObj = new ();

    private string? payloadAsString;
    private byte[]? payloadAsBytes;

    public T Payload { get; }

    public CachePayloadHolder(T payload, ILogger logger, KeyValuePair<string, object?> metricTag)
    {
        Payload = payload;
        this.logger = logger;
        this.metricTag = metricTag;
    }

    public string GetAsString()
    {
        // ReSharper disable once InconsistentlySynchronizedField
        if (payloadAsString is { } pas)
            return pas;

        lock (lockObj)
        {
            if (payloadAsString is not null)
                return payloadAsString;

            if (payloadAsBytes is { } pab)
            {
                pas = SmartCacheSerialization.Encoding.GetString(pab);
            }
            else
            {
                using (SmartCacheObservability.StartSerializeActivity(logger, metricTag))
                {
                    pas = SmartCacheSerialization.SerializeToString(Payload);
                }
            }

            return payloadAsString ??= pas;
        }
    }

    public byte[] GetAsBytes()
    {
        // ReSharper disable once InconsistentlySynchronizedField
        if (payloadAsBytes is { } pab)
            return pab;

        lock (lockObj)
        {
            if (payloadAsBytes is not null)
                return payloadAsBytes;

            if (payloadAsString is { } pas)
            {
                pab = SmartCacheSerialization.Encoding.GetBytes(pas);
            }
            else
            {
                using (SmartCacheObservability.StartSerializeActivity(logger, metricTag))
                {
                    pab = SmartCacheSerialization.SerializeToBytes(Payload);
                }
            }

            return payloadAsBytes ??= pab;
        }
    }
}
