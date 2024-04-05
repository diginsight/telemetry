namespace Diginsight.SmartCache.Externalization.ServiceBus;

public sealed class SmartCacheServiceBusOptions : ISmartCacheServiceBusOptions
{
    public string? ConnectionString { get; set; }

    string ISmartCacheServiceBusOptions.ConnectionString =>
        ConnectionString ?? throw new InvalidOperationException($"{nameof(ConnectionString)} is null");

    public string? TopicName { get; set; }

    string ISmartCacheServiceBusOptions.TopicName =>
        TopicName ?? throw new InvalidOperationException($"{nameof(TopicName)} is null");

    public string? SubscriptionName { get; set; }

    string ISmartCacheServiceBusOptions.SubscriptionName =>
        SubscriptionName ?? throw new InvalidOperationException($"{nameof(SubscriptionName)} is null");

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(5);
}
