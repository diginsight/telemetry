using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Diginsight.Diagnostics;
using Diginsight.SmartCache.Externalization.Redis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace Diginsight.SmartCache.Externalization.ServiceBus;

internal sealed class ServiceBusCacheCompanion : BackgroundService, ICacheCompanion
{
    private const string GetMessageSubject = "get";
    private const string GetReplyMessageSubject = "get reply";
    private const string CacheMissMessageSubject = "cachemiss";
    private const string InvalidateMessageSubject = "invalidate";

    private readonly ILogger<ServiceBusCacheCompanion> logger;
    private readonly ISmartCacheService cacheService;
    private readonly ISmartCacheServiceBusOptions serviceBusOptions;

    private readonly ManualResetEventSlim mre = new ();
    private readonly IEnumerable<CacheEventNotifier> eventNotifiers;

    public string SelfLocationId => serviceBusOptions.SubscriptionName;

    public IEnumerable<PassiveCacheLocation> PassiveLocations { get; }

    public ServiceBusCacheCompanion(
        ILogger<ServiceBusCacheCompanion> logger,
        ISmartCacheService cacheService,
        IOptions<SmartCacheServiceBusOptions> serviceBusOptions,
        RedisCacheLocation? redisLocation = null
    )
    {
        this.logger = logger;
        this.cacheService = cacheService;
        this.serviceBusOptions = serviceBusOptions.Value;

        eventNotifiers = new[] { new ServiceBusCacheEventNotifier(this) };

        PassiveLocations = redisLocation is null
            ? Enumerable.Empty<PassiveCacheLocation>()
            : new[] { redisLocation };
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await InstallAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    private async Task InstallAsync(CancellationToken cancellationToken)
    {
        string topicName = serviceBusOptions.TopicName;
        string subscriptionName = serviceBusOptions.SubscriptionName;
        const string ruleName = "$Default";

        cancellationToken.ThrowIfCancellationRequested();

        ServiceBusAdministrationClient administrationClient = new (serviceBusOptions.ConnectionString);
        if (await administrationClient.TopicExistsAsync(topicName, CancellationToken.None))
        {
            TopicProperties topicProperties = await administrationClient.GetTopicAsync(topicName, CancellationToken.None);
            topicProperties.AutoDeleteOnIdle = TimeSpan.FromDays(7);
            topicProperties.DefaultMessageTimeToLive = TimeSpan.FromHours(1);
            topicProperties.EnableBatchedOperations = true;

            await administrationClient.UpdateTopicAsync(topicProperties, CancellationToken.None);
        }
        else
        {
            await administrationClient.CreateTopicAsync(
                new CreateTopicOptions(topicName)
                {
                    AutoDeleteOnIdle = TimeSpan.FromDays(7),
                    DefaultMessageTimeToLive = TimeSpan.FromHours(1),
                    EnableBatchedOperations = true,
                    EnablePartitioning = true,
                },
                CancellationToken.None
            );
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (await administrationClient.SubscriptionExistsAsync(topicName, subscriptionName, CancellationToken.None))
        {
            SubscriptionProperties subscriptionProperties = await administrationClient.GetSubscriptionAsync(topicName, subscriptionName, CancellationToken.None);
            subscriptionProperties.AutoDeleteOnIdle = TimeSpan.FromDays(1);
            subscriptionProperties.DefaultMessageTimeToLive = TimeSpan.FromHours(1);
            subscriptionProperties.LockDuration = TimeSpan.FromSeconds(30);
            subscriptionProperties.DeadLetteringOnMessageExpiration = false;
            subscriptionProperties.EnableBatchedOperations = true;
            subscriptionProperties.EnableDeadLetteringOnFilterEvaluationExceptions = true;
            subscriptionProperties.MaxDeliveryCount = 2;

            await administrationClient.UpdateSubscriptionAsync(subscriptionProperties, CancellationToken.None);
        }
        else
        {
            await administrationClient.CreateSubscriptionAsync(
                new CreateSubscriptionOptions(topicName, subscriptionName)
                {
                    AutoDeleteOnIdle = TimeSpan.FromDays(1),
                    DefaultMessageTimeToLive = TimeSpan.FromHours(1),
                    LockDuration = TimeSpan.FromSeconds(30),
                    DeadLetteringOnMessageExpiration = false,
                    EnableBatchedOperations = true,
                    EnableDeadLetteringOnFilterEvaluationExceptions = true,
                    MaxDeliveryCount = 2,
                },
                CancellationToken.None
            );
        }

        string filterExpression = $"sys.To = '{subscriptionName}' OR (sys.To IS NULL AND sys.ReplyTo != '{subscriptionName}')";
        bool createRule = true;
        await foreach (RuleProperties ruleProperties in administrationClient.GetRulesAsync(topicName, subscriptionName, cancellationToken))
        {
            if (ruleProperties.Name == ruleName && ruleProperties.Filter is SqlRuleFilter filter && filter.SqlExpression == filterExpression)
            {
                createRule = false;
            }
            else
            {
                await administrationClient.DeleteRuleAsync(topicName, subscriptionName, ruleProperties.Name, CancellationToken.None);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (createRule)
        {
            await administrationClient.CreateRuleAsync(
                topicName,
                subscriptionName,
                new CreateRuleOptions(ruleName, new SqlRuleFilter(filterExpression)),
                CancellationToken.None
            );
        }
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        string topicName = serviceBusOptions.TopicName;
        string subscriptionName = serviceBusOptions.SubscriptionName;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await using ServiceBusClient client = new (serviceBusOptions.ConnectionString);
                await using ServiceBusReceiver receiver = client.CreateReceiver(topicName, subscriptionName);

                while (!cancellationToken.IsCancellationRequested)
                {
                    ServiceBusReceivedMessage? message;
                    try
                    {
                        message = await receiver.ReceiveMessageAsync(cancellationToken: cancellationToken);
                    }
                    catch (OperationCanceledException exception) when (exception.CancellationToken == cancellationToken)
                    {
                        break;
                    }
                    catch (Exception exception)
                    {
                        logger.LogWarning(exception, "Error receiving companion message");
                        await InstallAsync(cancellationToken);
                        break;
                    }

                    if (message is null)
                        continue;

                    await ProcessAsync(message);
                }
            }
        }
        finally
        {
            mre.Set();
        }
    }

    private Task ProcessAsync(ServiceBusReceivedMessage receivedMessage)
    {
        BinaryData body = receivedMessage.Body;
        string emitter = receivedMessage.ReplyTo;

        return receivedMessage.Subject.ToLowerInvariant() switch
        {
            GetMessageSubject => ProcessGetAsync(),
            GetReplyMessageSubject => ProcessGetReplyAsync(),
            CacheMissMessageSubject => ProcessCacheMissAsync(),
            InvalidateMessageSubject => ProcessInvalidateAsync(),
            _ => Task.CompletedTask,
        };

        async Task ProcessGetAsync()
        {
            ServiceBusMessage message = new ()
            {
                ReplyTo = serviceBusOptions.SubscriptionName,
                Subject = GetReplyMessageSubject,
                CorrelationId = receivedMessage.MessageId,
                To = emitter,
            };

            using (TimerLap lap = SmartCacheMetrics.Instruments.FetchDuration.StartLap(SmartCacheMetrics.Tags.Type.Direct))
            {
                ICacheKey key = DeserializeBody<ICacheKey>();
                if (cacheService.TryGetDirectFromMemory(key, out Type? type, out object? value))
                {
                    lap.AddTags(SmartCacheMetrics.Tags.Found.True);
                    message.Body = BinaryData.FromBytes(SmartCacheSerialization.SerializeToBytes(value, type));
                }
                else
                {
                    lap.AddTags(SmartCacheMetrics.Tags.Found.False);
                }
            }

            await GetSender().SendMessageAsync(message);
        }

        Task ProcessGetReplyAsync()
        {
            throw new NotImplementedException();
        }

        Task ProcessCacheMissAsync()
        {
            CacheMissDescriptor descriptor = DeserializeBody<CacheMissDescriptor>();
            cacheService.AddExternalMiss(descriptor);
            return Task.CompletedTask;
        }

        Task ProcessInvalidateAsync()
        {
            InvalidationDescriptor descriptor = DeserializeBody<InvalidationDescriptor>();
            cacheService.Invalidate(descriptor);
            return Task.CompletedTask;
        }

        T DeserializeBody<T>()
        {
            return SmartCacheSerialization.Deserialize<T>(body.ToArray());
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await base.StopAsync(cancellationToken);
        }
        finally
        {
            mre.Wait(CancellationToken.None);
            await UninstallAsync();
        }
    }

    private Task UninstallAsync()
    {
        ServiceBusAdministrationClient administrationClient = new (serviceBusOptions.ConnectionString);
        return administrationClient.DeleteSubscriptionAsync(serviceBusOptions.TopicName, serviceBusOptions.SubscriptionName);
    }

    public Task<IEnumerable<ActiveCacheLocation>> GetActiveLocationsAsync(IEnumerable<string> locationIds)
    {
        return Task.FromResult(locationIds.Select(x => new ServiceBusCacheLocation(x, this)).ToArray<ActiveCacheLocation>().AsEnumerable());
    }

    public Task<IEnumerable<CacheEventNotifier>> GetAllEventNotifiersAsync() => Task.FromResult(eventNotifiers);

    private ServiceBusClient GetClient() => throw new NotImplementedException();

    private ServiceBusSender GetSender() => throw new NotImplementedException();

    private sealed class ServiceBusCacheLocation : ActiveCacheLocation
    {
        private readonly ServiceBusCacheCompanion companion;

        public override KeyValuePair<string, object?> MetricTag => SmartCacheMetrics.Tags.Type.Distributed;

        public ServiceBusCacheLocation(string subscriptionName, ServiceBusCacheCompanion companion)
            : base(subscriptionName)
        {
            this.companion = companion;
        }

        public override async Task<CacheLocationOutput<TValue>?> GetAsync<TValue>(
            CacheKeyHolder keyHolder, DateTime minimumCreationDate, Action markInvalid, CancellationToken cancellationToken
        )
        {
            ServiceBusMessage message = new (keyHolder.GetAsBytes())
            {
                ReplyTo = companion.SelfLocationId,
                Subject = GetMessageSubject,
                To = Id,
            };

            await companion.GetSender().SendMessageAsync(message, CancellationToken.None);

            throw new NotImplementedException();
        }
    }

    private sealed class ServiceBusCacheEventNotifier : CacheEventNotifier
    {
        private readonly ServiceBusCacheCompanion companion;

        public ServiceBusCacheEventNotifier(ServiceBusCacheCompanion companion)
        {
            this.companion = companion;
        }

        protected override Task NotifyCacheMissAsync(CachePayloadHolder<CacheMissDescriptor> descriptorHolder)
        {
            return NotifyAsync(descriptorHolder, CacheMissMessageSubject);
        }

        protected override Task NotifyInvalidationAsync(CachePayloadHolder<InvalidationDescriptor> descriptorHolder)
        {
            return NotifyAsync(descriptorHolder, InvalidateMessageSubject);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Task NotifyAsync<T>(CachePayloadHolder<T> descriptorHolder, string subject)
            where T : notnull
        {
            ServiceBusMessage message = new (descriptorHolder.GetAsBytes())
            {
                ReplyTo = companion.SelfLocationId,
                Subject = subject,
            };

            return companion.GetSender().SendMessageAsync(message);
        }
    }
}
