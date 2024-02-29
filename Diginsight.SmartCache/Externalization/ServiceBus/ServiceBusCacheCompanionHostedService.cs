using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Diginsight.Diagnostics;
using Diginsight.SmartCache.Externalization.Redis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Diginsight.SmartCache.Externalization.ServiceBus;

internal sealed class ServiceBusCacheCompanionHostedService : BackgroundService, ICacheCompanionProvider
{
    private const string GetMessageSubject = "get";
    private const string GetReplyMessageSubject = "get reply";
    private const string CacheMissMessageSubject = "cachemiss";
    private const string InvalidateMessageSubject = "invalidate";

    private readonly ILogger<ServiceBusCacheCompanionHostedService> logger;
    private readonly ISmartCacheService cacheService;
    private readonly ISmartCacheServiceBusOptions serviceBusOptions;

    private readonly ManualResetEventSlim mre = new ();

    public string SelfLocationId => serviceBusOptions.SubscriptionName;

    public IEnumerable<PassiveCacheLocation> PassiveLocations { get; }

    public ServiceBusCacheCompanionHostedService(
        ILogger<ServiceBusCacheCompanionHostedService> logger,
        ISmartCacheService cacheService,
        IOptions<SmartCacheServiceBusOptions> serviceBusOptions,
        RedisCacheLocation? redisLocation = null
    )
    {
        this.logger = logger;
        this.cacheService = cacheService;
        this.serviceBusOptions = serviceBusOptions.Value;

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
                CorrelationId = receivedMessage.MessageId,
                ReplyTo = serviceBusOptions.SubscriptionName,
                To = emitter,
                Subject = GetReplyMessageSubject,
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

    private ServiceBusClient GetClient() => throw new NotImplementedException();

    private ServiceBusSender GetSender() => throw new NotImplementedException();

    public Task<IEnumerable<CacheCompanion>> GetCompanionsAsync()
    {
        throw new NotImplementedException();
    }

    private sealed class CacheCompanionImpl : CacheCompanion
    {
        private readonly ServiceBusCacheCompanionHostedService hostedService;

        public CacheCompanionImpl(
            ServiceBusCacheCompanionHostedService hostedService
        )
            : base("<others>")
        {
            this.hostedService = hostedService;
        }

        public override Task<CacheLocationOutput<TValue>?> GetAsync<TValue>(
            CacheKeyHolder keyHolder, DateTime minimumCreationDate, Action markInvalid, CancellationToken cancellationToken
        )
        {
            throw new UnreachableException();
        }

        protected override Task PublishCacheMissAsync(CachePayloadHolder<CacheMissDescriptor> descriptorHolder)
        {
            return PublishAsync(descriptorHolder, CacheMissMessageSubject);
        }

        protected override Task PublishInvalidationAsync(CachePayloadHolder<InvalidationDescriptor> descriptorHolder)
        {
            return PublishAsync(descriptorHolder, InvalidateMessageSubject);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Task PublishAsync<T>(CachePayloadHolder<T> descriptorHolder, string subject)
            where T : notnull
        {
            ServiceBusMessage message = new (descriptorHolder.GetAsBytes())
            {
                ReplyTo = hostedService.SelfLocationId,
                Subject = subject,
            };

            return hostedService.GetSender().SendMessageAsync(message);
        }
    }
}
