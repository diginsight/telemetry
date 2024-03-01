using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Diginsight.Diagnostics;
using Diginsight.SmartCache.Externalization.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Diginsight.SmartCache.Externalization.ServiceBus;

internal sealed class ServiceBusCacheCompanion : BackgroundService, ICacheCompanion
{
    private const string GetMessageSubject = "get";
    private const string GetReplyMessageSubject = "get reply";
    private const string CacheMissMessageSubject = "cachemiss";
    private const string InvalidateMessageSubject = "invalidate";

    private readonly ILogger<ServiceBusCacheCompanion> logger;
    private readonly Lazy<ISmartCacheService> cacheServiceLazy;
    private readonly IServiceProvider serviceProvider;
    private readonly ISmartCacheServiceBusOptions serviceBusOptions;

    private readonly IEnumerable<CacheEventNotifier> eventNotifiers;
    private readonly ClientHolder clientHolder;
    private readonly ReplyDictionary replyDictionary;
    private readonly ManualResetEventSlim processMre = new ();
    private readonly ObjectFactory<ServiceBusCacheLocation> makeLocation =
        ActivatorUtilities.CreateFactory<ServiceBusCacheLocation>([ typeof(string) ]);

    public string SelfLocationId => serviceBusOptions.SubscriptionName;

    public IEnumerable<PassiveCacheLocation> PassiveLocations { get; }

    private ISmartCacheService CacheService => cacheServiceLazy.Value;

    public ServiceBusCacheCompanion(
        ILogger<ServiceBusCacheCompanion> logger,
        Lazy<ISmartCacheService> cacheServiceLazy,
        IServiceProvider serviceProvider,
        IOptions<SmartCacheServiceBusOptions> serviceBusOptions,
        TimeProvider? timeProvider = null,
        RedisCacheLocation? redisLocation = null
    )
    {
        this.logger = logger;
        this.cacheServiceLazy = cacheServiceLazy;
        this.serviceProvider = serviceProvider;
        this.serviceBusOptions = serviceBusOptions.Value;

        eventNotifiers = new[] { new ServiceBusCacheEventNotifier(this) };
        clientHolder = new ClientHolder(this.serviceBusOptions);
        replyDictionary = new ReplyDictionary(timeProvider ?? TimeProvider.System);

        PassiveLocations = redisLocation is null
            ? Enumerable.Empty<PassiveCacheLocation>()
            : new[] { redisLocation };
    }

    private sealed class ClientHolder
    {
        private readonly ISmartCacheServiceBusOptions serviceBusOptions;
        private readonly ManualResetEventSlim validMre = new (true);

        private ServiceBusClient? client;
        private ServiceBusSender? sender;

        public ServiceBusClient Client
        {
            get
            {
                validMre.Wait();
                return client!;
            }
        }

        public ServiceBusSender Sender
        {
            get
            {
                validMre.Wait();
                return sender!;
            }
        }

        public ClientHolder(ISmartCacheServiceBusOptions serviceBusOptions)
        {
            this.serviceBusOptions = serviceBusOptions;
        }

        public void Invalidate()
        {
            validMre.Reset();
            client = null;
            sender = null;
        }

        public void Initialize()
        {
            client = new ServiceBusClient(serviceBusOptions.ConnectionString);
            sender = client.CreateSender(serviceBusOptions.TopicName);
            validMre.Set();
        }
    }

    private sealed class ReplyDictionary : IDisposable
    {
        private readonly TimeProvider timeProvider;
        private readonly ConcurrentDictionary<string, Entry> underlying = new ();
        private readonly Timer cleanupTimer;

        public ReplyDictionary(TimeProvider timeProvider)
        {
            this.timeProvider = timeProvider;
            TimeSpan cleanupPeriod = TimeSpan.FromMinutes(1);
            cleanupTimer = new Timer(Cleanup, null, cleanupPeriod, cleanupPeriod);

            void Cleanup(object? state)
            {
                DateTimeOffset now = timeProvider.GetUtcNow();
                foreach ((string messageId, Entry entry) in underlying)
                {
                    if (now - entry.Timestamp > cleanupPeriod)
                    {
                        _ = underlying.TryRemove(messageId, out _);
                    }
                }
            }
        }

        public BinaryData Get(string messageId, CancellationToken cancellationToken)
        {
            return InnerGetOrAdd(messageId).Get(cancellationToken);
        }

        public void Set(string messageId, BinaryData body)
        {
            InnerGetOrAdd(messageId).Set(body);
        }

        private Entry InnerGetOrAdd(string messageId)
        {
            return underlying.GetOrAdd(
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                messageId, static (_, a) => new Entry(a.GetUtcNow()), timeProvider
#else
                messageId, _ => new Entry(timeProvider.GetUtcNow())
#endif
            );
        }

        public void Dispose()
        {
            cleanupTimer.Dispose();
            underlying.Clear();
        }

        private sealed class Entry
        {
            private readonly ManualResetEventSlim mre = new ();
            private BinaryData? body;

            public DateTimeOffset Timestamp { get; }

            public Entry(DateTimeOffset timestamp)
            {
                Timestamp = timestamp;
            }

            public BinaryData Get(CancellationToken cancellationToken)
            {
                mre.Wait(cancellationToken);
                return body!;
            }

            public void Set(BinaryData value)
            {
                if (body is not null)
                    return;

                body = value;
                mre.Set();
            }
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await InstallAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    private async Task InstallAsync(CancellationToken cancellationToken)
    {
        using Activity? activity = SmartCacheMetrics.ActivitySource.StartMethodActivity(logger);

        string topicName = serviceBusOptions.TopicName;
        string subscriptionName = serviceBusOptions.SubscriptionName;
        const string ruleName = "$Default";

        ServiceBusAdministrationClient administrationClient = new (serviceBusOptions.ConnectionString);

        cancellationToken.ThrowIfCancellationRequested();

        logger.LogDebug("Installing topic '{TopicName}'", topicName);
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

        logger.LogDebug("Installing subscription '{SubscriptionName}'", subscriptionName);

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

        logger.LogDebug("Installing subscription rule");

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

        clientHolder.Initialize();
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        string topicName = serviceBusOptions.TopicName;
        string subscriptionName = serviceBusOptions.SubscriptionName;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                logger.LogDebug("Starting messages listen loop");

                try
                {
                    await using ServiceBusClient client = clientHolder.Client;
                    await using ServiceBusReceiver receiver = client.CreateReceiver(topicName, subscriptionName);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        ServiceBusReceivedMessage? message;
                        try
                        {
                            message = await receiver.ReceiveMessageAsync(maxWaitTime: TimeSpan.FromSeconds(10), cancellationToken);
                        }
                        catch (Exception exception)
                        {
                            if (exception is not OperationCanceledException)
                            {
                                logger.LogWarning(exception, "Error receiving companion message");
                            }
                            break;
                        }

                        if (message is null)
                            continue;

                        await ProcessAsync(message);
                    }
                }
                finally
                {
                    clientHolder.Invalidate();
                }

                if (cancellationToken.IsCancellationRequested)
                    break;

                await InstallAsync(cancellationToken);
            }
        }
        finally
        {
            processMre.Set();
        }
    }

    private async Task ProcessAsync(ServiceBusReceivedMessage receivedMessage)
    {
        string emitter = receivedMessage.ReplyTo;
        string subject = receivedMessage.Subject.ToLowerInvariant();

        using Activity? activity = SmartCacheMetrics.ActivitySource.StartMethodActivity(logger, new { emitter, subject });

        BinaryData body = receivedMessage.Body;

        await (subject switch
        {
            GetMessageSubject => ProcessGetAsync(),
            GetReplyMessageSubject => ProcessGetReplyAsync(),
            CacheMissMessageSubject => ProcessCacheMissAsync(),
            InvalidateMessageSubject => ProcessInvalidateAsync(),
            _ => Task.CompletedTask,
        });

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
                if (CacheService.TryGetDirectFromMemory(key, out Type? type, out object? value))
                {
                    lap.AddTags(SmartCacheMetrics.Tags.Found.True);
                    message.Body = BinaryData.FromBytes(SmartCacheSerialization.SerializeToBytes(value, type));
                }
                else
                {
                    lap.AddTags(SmartCacheMetrics.Tags.Found.False);
                }
            }

            await clientHolder.Sender.SendMessageAsync(message);
        }

        Task ProcessGetReplyAsync()
        {
            replyDictionary.Set(receivedMessage.MessageId, receivedMessage.Body);
            return Task.CompletedTask;
        }

        Task ProcessCacheMissAsync()
        {
            CacheMissDescriptor descriptor = DeserializeBody<CacheMissDescriptor>();
            CacheService.AddExternalMiss(descriptor);
            return Task.CompletedTask;
        }

        Task ProcessInvalidateAsync()
        {
            InvalidationDescriptor descriptor = DeserializeBody<InvalidationDescriptor>();
            CacheService.Invalidate(descriptor);
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
            processMre.Wait(CancellationToken.None);
            await UninstallAsync();
        }
    }

    private async Task UninstallAsync()
    {
        using Activity? activity = SmartCacheMetrics.ActivitySource.StartMethodActivity(logger);

        ServiceBusAdministrationClient administrationClient = new (serviceBusOptions.ConnectionString);

        string subscriptionName = serviceBusOptions.SubscriptionName;
        logger.LogDebug("Deleting subscription '{SubscriptionName}'", subscriptionName);
        await administrationClient.DeleteSubscriptionAsync(serviceBusOptions.TopicName, subscriptionName);
    }

    public override void Dispose()
    {
        base.Dispose();
        replyDictionary.Dispose();
    }

    public Task<IEnumerable<ActiveCacheLocation>> GetActiveLocationsAsync(IEnumerable<string> locationIds)
    {
        return Task.FromResult(
            locationIds
                .Select(x => makeLocation(serviceProvider, [ x ]))
                .ToArray<ActiveCacheLocation>()
                .AsEnumerable()
        );
    }

    public Task<IEnumerable<CacheEventNotifier>> GetAllEventNotifiersAsync() => Task.FromResult(eventNotifiers);

    private sealed class ServiceBusCacheLocation : ActiveCacheLocation
    {
        private readonly ILogger logger;
        private readonly ServiceBusCacheCompanion companion;

        public override KeyValuePair<string, object?> MetricTag => SmartCacheMetrics.Tags.Type.Distributed;

        public ServiceBusCacheLocation(
            string subscriptionName,
            ILogger<ServiceBusCacheLocation> logger,
            ServiceBusCacheCompanion companion
        )
            : base(subscriptionName)
        {
            this.logger = logger;
            this.companion = companion;
        }

        public override async Task<CacheLocationOutput<TValue>?> GetAsync<TValue>(
            CacheKeyHolder keyHolder, DateTime minimumCreationDate, Action markInvalid, CancellationToken cancellationToken
        )
        {
            using Activity? activity = SmartCacheMetrics.ActivitySource.StartMethodActivity(logger, new { key = keyHolder.Key, minimumCreationDate });

            using TimerLap lap = SmartCacheMetrics.Instruments.FetchDuration.CreateLap(SmartCacheMetrics.Tags.Type.Distributed);

            string messageId = Guid.NewGuid().ToString("N");
            ServiceBusMessage message = new (keyHolder.GetAsBytes())
            {
                ReplyTo = companion.SelfLocationId,
                Subject = GetMessageSubject,
                MessageId = messageId,
                To = Id,
            };

            byte[] body;
            using (lap.Start())
            {
                await companion.clientHolder.Sender.SendMessageAsync(message, CancellationToken.None);

                try
                {
                    CancellationToken combinedCancellationToken = CancellationTokenSource
                        .CreateLinkedTokenSource(cancellationToken, new CancellationTokenSource(companion.serviceBusOptions.CompanionRequestTimeout).Token)
                        .Token;
                    body = companion.replyDictionary.Get(messageId, combinedCancellationToken).ToArray();
                }
                catch (OperationCanceledException oce) when (oce.CancellationToken != cancellationToken)
                {
                    body = Array.Empty<byte>();
                }
            }

            long valueSerializedSize = body.LongLength;
            if (!(valueSerializedSize > 0))
            {
                markInvalid();
                logger.LogDebug("Partial cache miss: Failed to retrieve value from peer '{PeerId}'", Id);

                lap.AddTags(SmartCacheMetrics.Tags.Found.False);
                return null;
            }

            TValue item;
            using (SmartCacheMetrics.StartDeserializeActivity(logger, SmartCacheMetrics.Tags.Subject.Value))
            {
                item = SmartCacheSerialization.Deserialize<TValue>(body);
            }

            double latencyMsecD = lap.ElapsedMilliseconds;
            long latencyMsecL = (long)latencyMsecD;

            logger.LogDebug("Cache hit (latency: {LatencyMsec}): Returning up-to-date value from peer '{PeerId}'", latencyMsecL, Id);

            lap.AddTags(SmartCacheMetrics.Tags.Found.True);
            return new CacheLocationOutput<TValue>(item, valueSerializedSize, latencyMsecD);
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

            return companion.clientHolder.Sender.SendMessageAsync(message);
        }
    }
}
