using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Diginsight.Diagnostics;
using Diginsight.SmartCache.Externalization.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Diginsight.SmartCache.Externalization.ServiceBus;

// TODO Dispose ManualResetEventSlim
internal sealed class ServiceBusCacheCompanion : BackgroundService, ICacheCompanion
{
    private const string SourcePropertyName = "source";
    private const string DestinationPropertyName = "destination";

    private const string GetRequestSubject = "get_request";
    private const string GetResponseMessageSubject = "get_response";
    private const string CacheMissMessageSubject = "cachemiss";
    private const string InvalidateMessageSubject = "invalidate";

    private readonly ILogger<ServiceBusCacheCompanion> logger;
    private readonly Lazy<ISmartCache> smartCacheLazy;
    private readonly IServiceProvider serviceProvider;
    private readonly ISmartCacheServiceBusOptions serviceBusOptions;

    private readonly ClientHolder clientHolder;
    private readonly ReplyDictionary replyDictionary;
    private readonly ManualResetEventSlim processMre = new ();
    private readonly ObjectFactory<ServiceBusCacheLocation> makeLocation =
        ActivatorUtilities.CreateFactory<ServiceBusCacheLocation>([ typeof(string) ]);

    private IEnumerable<CacheEventNotifier>? eventNotifiers;

    public string SelfLocationId => serviceBusOptions.SubscriptionName;

    public IEnumerable<PassiveCacheLocation> PassiveLocations { get; }

    private ISmartCache SmartCache => smartCacheLazy.Value;

    public ServiceBusCacheCompanion(
        ILogger<ServiceBusCacheCompanion> logger,
        Lazy<ISmartCache> smartCacheLazy,
        IServiceProvider serviceProvider,
        IOptions<SmartCacheServiceBusOptions> serviceBusOptions,
        TimeProvider? timeProvider = null,
        RedisCacheLocation? redisLocation = null
    )
    {
        this.logger = logger;
        this.smartCacheLazy = smartCacheLazy;
        this.serviceProvider = serviceProvider;
        this.serviceBusOptions = serviceBusOptions.Value;

        clientHolder = new ClientHolder(this.serviceBusOptions);
        replyDictionary = new ReplyDictionary(timeProvider ?? TimeProvider.System);

        PassiveLocations = redisLocation is null
            ? Enumerable.Empty<PassiveCacheLocation>()
            : new[] { redisLocation };
    }

    private sealed class ClientHolder
    {
        private readonly ISmartCacheServiceBusOptions serviceBusOptions;
        private readonly ManualResetEventSlim validMre = new ();

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
                DateTime now = timeProvider.GetUtcNow().UtcDateTime;
                foreach ((string messageId, Entry entry) in underlying)
                {
                    if (now - entry.Timestamp > cleanupPeriod)
                    {
                        _ = underlying.TryRemove(messageId, out _);
                    }
                }
            }
        }

        public Task<byte[]> GetAsync(string messageId, CancellationToken cancellationToken)
        {
            return InnerGetOrAdd(messageId).GetAsync(cancellationToken);
        }

        public void Set(string messageId, byte[] body)
        {
            InnerGetOrAdd(messageId).Set(body);
        }

        private Entry InnerGetOrAdd(string messageId)
        {
            return underlying.GetOrAdd(
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                messageId, static (_, a) => new Entry(a.GetUtcNow().UtcDateTime), timeProvider
#else
                messageId, _ => new Entry(timeProvider.GetUtcNow().UtcDateTime)
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
            private byte[]? body;

            public DateTime Timestamp { get; }

            public Entry(DateTime timestamp)
            {
                Timestamp = timestamp;
            }

            public Task<byte[]> GetAsync(CancellationToken cancellationToken)
            {
                TaskCompletionSource<byte[]> tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
                _ = Task.Run(GetCore, CancellationToken.None);
                return tcs.Task;

                void GetCore()
                {
                    try
                    {
                        mre.Wait(cancellationToken);
                    }
                    catch (OperationCanceledException exception) when (exception.CancellationToken == cancellationToken)
                    {
#if NET6_0_OR_GREATER
                        tcs.SetCanceled(cancellationToken);
#else
                        if (!tcs.TrySetCanceled(cancellationToken))
                        {
                            throw new InvalidOperationException("Task already in final state");
                        }
#endif
                        return;
                    }
                    catch (Exception exception)
                    {
                        tcs.SetException(exception);
                        return;
                    }

                    tcs.SetResult(body!);
                }
            }

            // ReSharper disable once ParameterHidesMember
            public void Set(byte[] body)
            {
                if (this.body is not null)
                    return;

                this.body = body;
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

        RetryStrategyOptions retryStrategyOptions = new ()
        {
            ShouldHandle = static args =>
            {
                Exception exception = args.Outcome.Exception!;
                bool shouldHandle = exception is ServiceBusException sbException &&
                    (sbException.IsTransient ||
                        sbException.Reason is ServiceBusFailureReason.MessagingEntityAlreadyExists or ServiceBusFailureReason.MessagingEntityNotFound);
                return new ValueTask<bool>(shouldHandle);
            },
        };

        ResiliencePipeline resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(retryStrategyOptions)
            .Build();

        await resiliencePipeline.ExecuteAsync(InstallTopicAsync, cancellationToken);
        await resiliencePipeline.ExecuteAsync(InstallSubscriptionAsync, cancellationToken);
        await resiliencePipeline.ExecuteAsync(InstallRuleAsync, cancellationToken);

        clientHolder.Initialize();

        async ValueTask InstallTopicAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

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
        }

        async ValueTask InstallSubscriptionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

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
        }

        async ValueTask InstallRuleAsync(CancellationToken ct)
        {
            logger.LogDebug("Installing subscription rule");

            string filterExpression = $"[{SourcePropertyName}] != '{subscriptionName}' AND ((NOT EXISTS ([{DestinationPropertyName}])) OR [{DestinationPropertyName}] = '{subscriptionName}')";
            bool createRule = true;
            await foreach (RuleProperties ruleProperties in administrationClient.GetRulesAsync(topicName, subscriptionName, ct))
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

            ct.ThrowIfCancellationRequested();

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
                        ServiceBusReceivedMessage? receivedMessage;
                        try
                        {
                            receivedMessage = await receiver.ReceiveMessageAsync(maxWaitTime: TimeSpan.FromSeconds(10), cancellationToken);
                        }
                        catch (Exception exception)
                        {
                            if (exception is not OperationCanceledException)
                            {
                                logger.LogWarning(exception, "Error receiving companion message");
                            }
                            break;
                        }

                        if (receivedMessage is null)
                            continue;

                        await ProcessAsync(receiver, receivedMessage);
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

    private async Task ProcessAsync(ServiceBusReceiver receiver, ServiceBusReceivedMessage receivedMessage)
    {
        using Activity? activity = SmartCacheMetrics.ActivitySource.StartMethodActivity(logger);

        if (receivedMessage.ApplicationProperties.GetValueOrDefault(SourcePropertyName) is not string emitter)
        {
            logger.LogDebug("Received message without emitter; will be discarded");
            await receiver.AbandonMessageAsync(receivedMessage);
            return;
        }

        string subject = receivedMessage.Subject?.ToLowerInvariant() ?? "";
        logger.LogDebug("Received message from '{Emitter}' with subject '{Subject}'", emitter, subject);

        StrongBox<bool> completedBox = new (false);

        try
        {
            async Task CompleteMessageAsync()
            {
                await receiver.CompleteMessageAsync(receivedMessage);
                completedBox.Value = true;
            }

            T DeserializeBody<T>()
            {
                return SmartCacheSerialization.Deserialize<T>(receivedMessage.Body.ToArray());
            }

            async Task ProcessGetAsync()
            {
                logger.LogDebug("Sending message for get reply to '{Destination}'", emitter);

                ServiceBusMessage message;
                using (TimerLap lap = SmartCacheMetrics.Instruments.FetchDuration.StartLap(SmartCacheMetrics.Tags.Type.Direct))
                {
                    ICacheKey key = DeserializeBody<ICacheKey>();
                    await CompleteMessageAsync();

                    message = new ServiceBusMessage()
                    {
                        Subject = GetResponseMessageSubject,
                        CorrelationId = receivedMessage.MessageId,
                        ApplicationProperties =
                        {
                            [SourcePropertyName] = serviceBusOptions.SubscriptionName,
                            [DestinationPropertyName] = emitter,
                        },
                    };

                    if (SmartCache.TryGetDirectFromMemory(key, out Type? type, out object? value))
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

            async Task ProcessGetReplyAsync()
            {
                replyDictionary.Set(receivedMessage.CorrelationId, receivedMessage.Body.ToArray());
                await CompleteMessageAsync();
            }

            async Task ProcessCacheMissAsync()
            {
                CacheMissDescriptor descriptor = DeserializeBody<CacheMissDescriptor>();
                await CompleteMessageAsync();
                SmartCache.AddExternalMiss(descriptor);
            }

            async Task ProcessInvalidateAsync()
            {
                InvalidationDescriptor descriptor = DeserializeBody<InvalidationDescriptor>();
                await CompleteMessageAsync();
                SmartCache.Invalidate(descriptor);
            }

            await (subject switch
            {
                GetRequestSubject => ProcessGetAsync(),
                GetResponseMessageSubject => ProcessGetReplyAsync(),
                CacheMissMessageSubject => ProcessCacheMissAsync(),
                InvalidateMessageSubject => ProcessInvalidateAsync(),
                _ => Task.CompletedTask,
            });
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Error processing '{Subject}' message", subject);
            if (!completedBox.Value)
            {
                await receiver.DeadLetterMessageAsync(receivedMessage, "ExceptionProcessing", exception.Message);
                completedBox.Value = true;
            }
        }
        finally
        {
            if (!completedBox.Value)
            {
                await receiver.AbandonMessageAsync(receivedMessage);
            }
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

        string topicName = serviceBusOptions.TopicName;
        string subscriptionName = serviceBusOptions.SubscriptionName;

        logger.LogDebug("Deleting subscription '{SubscriptionName}'", subscriptionName);
        if (await administrationClient.SubscriptionExistsAsync(topicName, subscriptionName))
        {
            await administrationClient.DeleteSubscriptionAsync(topicName, subscriptionName);
        }
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

    public Task<IEnumerable<CacheEventNotifier>> GetAllEventNotifiersAsync()
    {
        return Task.FromResult(eventNotifiers ??= [ ActivatorUtilities.CreateInstance<ServiceBusCacheEventNotifier>(serviceProvider) ]);
    }

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
            logger.LogDebug("Sending message for get request to '{Destination}'", Id);

            using TimerLap lap = SmartCacheMetrics.Instruments.FetchDuration.CreateLap(SmartCacheMetrics.Tags.Type.Distributed);

            string messageId = Guid.NewGuid().ToString("N");
            ServiceBusMessage message = new (keyHolder.GetAsBytes())
            {
                Subject = GetRequestSubject,
                MessageId = messageId,
                ApplicationProperties =
                {
                    [SourcePropertyName] = companion.SelfLocationId,
                    [DestinationPropertyName] = Id,
                },
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
                    body = await companion.replyDictionary.GetAsync(messageId, combinedCancellationToken);
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
        private readonly ILogger logger;
        private readonly ServiceBusCacheCompanion companion;

        public ServiceBusCacheEventNotifier(
            ILogger<ServiceBusCacheEventNotifier> logger,
            ServiceBusCacheCompanion companion
        )
        {
            this.logger = logger;
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
            logger.LogDebug("Sending message for '{Subject}' event notification", subject);

            ServiceBusMessage message = new (descriptorHolder.GetAsBytes())
            {
                Subject = subject,
                ApplicationProperties =
                {
                    [SourcePropertyName] = companion.SelfLocationId,
                },
            };

            return companion.clientHolder.Sender.SendMessageAsync(message);
        }
    }
}
