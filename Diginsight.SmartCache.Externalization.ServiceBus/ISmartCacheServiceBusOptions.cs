namespace Diginsight.SmartCache.Externalization.ServiceBus;

public interface ISmartCacheServiceBusOptions
{
    string ConnectionString { get; }
    string TopicName { get; }
    string SubscriptionName { get; }
    TimeSpan RequestTimeout { get; }
}
