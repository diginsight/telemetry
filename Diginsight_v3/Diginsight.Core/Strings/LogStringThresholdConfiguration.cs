﻿namespace Diginsight.Strings;

public sealed class LogStringThresholdConfiguration : ILogStringThresholdConfiguration
{
    private LogThreshold maxDepth;

    public LogThreshold MaxCollectionItemCount { get; set; }
    public InheritableLogThreshold MaxDictionaryItemCount { get; set; }
    public InheritableLogThreshold MaxMemberwisePropertyCount { get; set; }
    public InheritableLogThreshold MaxAnonymousObjectPropertyCount { get; set; }
    public LogThreshold MaxTupleItemCount { get; set; }
    public LogThreshold MaxMethodParameterCount { get; set; }

    public LogThreshold MaxDepth
    {
        get => maxDepth;
        set => maxDepth = value.Value == 0 ? throw new ArgumentOutOfRangeException(nameof(MaxDepth), "expected positive value") : value;
    }

    public int? EffectiveMaxCollectionItemCount => MaxCollectionItemCount.Value;

    public int? EffectiveMaxDictionaryItemCount =>
        MaxDictionaryItemCount.GetValue(MaxCollectionItemCount);

    public int? EffectiveMaxMemberwisePropertyCount =>
        MaxMemberwisePropertyCount.GetValue(MaxCollectionItemCount, MaxDictionaryItemCount);

    public int? EffectiveMaxAnonymousObjectPropertyCount =>
        MaxAnonymousObjectPropertyCount.GetValue(MaxCollectionItemCount, MaxDictionaryItemCount, MaxMemberwisePropertyCount);

    public int? EffectiveMaxTupleItemCount => MaxTupleItemCount.Value;

    public int? EffectiveMaxMethodParameterCount => MaxMethodParameterCount.Value;

    public int? EffectiveMaxDepth => MaxDepth.Value;

    public LogStringThresholdConfiguration(ILogStringThresholdConfiguration source)
    {
        MaxCollectionItemCount = source.MaxCollectionItemCount;
        MaxDictionaryItemCount = source.MaxDictionaryItemCount;
        MaxMemberwisePropertyCount = source.MaxMemberwisePropertyCount;
        MaxAnonymousObjectPropertyCount = source.MaxAnonymousObjectPropertyCount;
        MaxTupleItemCount = source.MaxTupleItemCount;
        MaxMethodParameterCount = source.MaxMethodParameterCount;
        MaxDepth = source.MaxDepth;
    }
}