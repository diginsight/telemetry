﻿using Diginsight.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Diginsight.SmartCache;

internal static class SmartCacheObservability
{
    public static readonly ActivitySource ActivitySource = new (typeof(SmartCacheObservability).Namespace!);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? StartSerializeActivity(ILogger logger, params KeyValuePair<string, object?>[] tags)
    {
        Activity? activity = ActivitySource.StartRichActivity(logger, $"{nameof(SmartCache)}.Serialize", stackDepth: 1);
        activity?.SetCustomDurationMetric(Instruments.SerializationDuration, [ Tags.Operation.Serialization, ..tags ]);
        return activity;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Activity? StartDeserializeActivity(ILogger logger, params KeyValuePair<string, object?>[] tags)
    {
        Activity? activity = ActivitySource.StartRichActivity(logger, $"{nameof(SmartCache)}.Deserialize", stackDepth: 1);
        activity?.SetCustomDurationMetric(Instruments.SerializationDuration, [ Tags.Operation.Deserialization, ..tags ]);
        return activity;
    }

    public static class Instruments
    {
        public static readonly TimerHistogram FetchDuration;
        public static readonly Histogram<double> SerializationDuration;
        public static readonly TimerHistogram SizeComputationDuration;
        public static readonly TimerHistogram CompanionFetchDuration;
        public static readonly Histogram<double> CompanionFetchRelativeDuration;
        public static readonly Counter<int> Sources;
        public static readonly Counter<int> Calls;
        public static readonly Counter<int> Preloads;
        public static readonly Counter<int> Evictions;
        public static readonly Histogram<long> KeyObjectSize;
        public static readonly Histogram<long> ValueObjectSize;
        public static readonly Histogram<long> KeySerializedSize;
        public static readonly Histogram<long> ValueSerializedSize;
        public static readonly UpDownCounter<long> TotalSize;

        static Instruments()
        {
            Meter meter = new (ActivitySource.Name);

            FetchDuration = meter.CreateTimer("cache.fetch.duration");
            SerializationDuration = meter.CreateHistogram<double>("cache.serialization.duration");
            SizeComputationDuration = meter.CreateTimer("cache.size_computation.duration");
            CompanionFetchDuration = meter.CreateTimer("cache.companion_fetch.duration");
            CompanionFetchRelativeDuration = meter.CreateHistogram<double>("cache.companion_fetch.relative_duration", "ms_per_kbyte");
            Sources = meter.CreateCounter<int>("cache.source.count");
            Calls = meter.CreateCounter<int>("cache.call.count");
            Preloads = meter.CreateCounter<int>("cache.preload.count");
            Evictions = meter.CreateCounter<int>("cache.eviction.count");
            KeyObjectSize = meter.CreateHistogram<long>("cache.key.object_size", "vbytes");
            ValueObjectSize = meter.CreateHistogram<long>("cache.value.object_size", "vbytes");
            KeySerializedSize = meter.CreateHistogram<long>("cache.key.serialized_size", "bytes");
            ValueSerializedSize = meter.CreateHistogram<long>("cache.value.serialized_size", "bytes");
            TotalSize = meter.CreateUpDownCounter<long>("cache.total_size", "vbytes");
        }
    }

    public static class Tags
    {
        public static class Found
        {
            public static readonly KeyValuePair<string, object?> True = new ("found", true);
            public static readonly KeyValuePair<string, object?> False = new ("found", false);
        }

        public static class Type
        {
            public static readonly KeyValuePair<string, object?> Memory = new ("source_type", "memory");
            public static readonly KeyValuePair<string, object?> Distributed = new ("source_type", "distributed");
            public static readonly KeyValuePair<string, object?> Redis = new ("source_type", "redis");
            public static readonly KeyValuePair<string, object?> Miss = new ("source_type", "miss");
            public static readonly KeyValuePair<string, object?> Disabled = new ("source_type", "disabled");
            public static readonly KeyValuePair<string, object?> Preload = new ("source_type", "preload");
            public static readonly KeyValuePair<string, object?> Direct = new ("source_type", "direct");
        }

        public static class Eviction
        {
            public static readonly KeyValuePair<string, object?> Expired = new ("eviction_reason", "expired");
            public static readonly KeyValuePair<string, object?> Capacity = new ("eviction_reason", "capacity");
            public static readonly KeyValuePair<string, object?> Removed = new ("eviction_reason", "removed");
            public static readonly KeyValuePair<string, object?> Replaced = new ("eviction_reason", "replaced");
        }

        public static class Subject
        {
            public static readonly KeyValuePair<string, object?> Key = new ("subject", "cache_key");
            public static readonly KeyValuePair<string, object?> Value = new ("subject", "cache_value");
        }

        public static class Operation
        {
            public static readonly KeyValuePair<string, object?> Serialization = new ("operation", "serialization");
            public static readonly KeyValuePair<string, object?> Deserialization = new ("operation", "deserialization");
        }
    }
}
