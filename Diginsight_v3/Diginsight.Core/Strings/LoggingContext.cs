using System.Text;

namespace Diginsight.Strings;

public sealed class LoggingContext
{
    private readonly IEnumerable<ILogStringProvider> logStringProviders;
    private readonly ISet<object> renderedObjs = new HashSet<object>(ReferenceEqualityComparer.Instance);

    private LogStringThresholdConfiguration thresholdConfiguration;
    private Dictionary<string, object?> metaProperties;
    private int currentDepth = 0;

    public IReadOnlyDictionary<string, object?> MetaProperties => metaProperties;

    internal LoggingContext(
        IEnumerable<ILogStringProvider> logStringProviders,
        LogStringThresholdConfiguration thresholdConfiguration,
        IEqualityComparer<string> metaPropertyKeyComparer
    )
    {
        this.logStringProviders = logStringProviders;
        this.thresholdConfiguration = thresholdConfiguration;
        metaProperties = new Dictionary<string, object?>(metaPropertyKeyComparer);
    }

    public void Append(
        object? obj,
        StringBuilder stringBuilder,
        bool incrementDepth = true,
        Action<LogStringThresholdConfiguration>? configureThresholds = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        if (obj == null)
        {
            stringBuilder.Append('□');
            return;
        }

        using IDisposable? _0 = WithThresholdsSafe(configureThresholds);
        using IDisposable? _1 = WithMetaPropertiesSafe(configureMetaProperties);
        using IDisposable? _2 = this.IncrementDepth(incrementDepth, out bool isMaxDepth);

        ILoggable? loggable = obj as ILoggable;
        if (loggable is null)
        {
            foreach (ILogStringProvider logStringProvider in logStringProviders)
            {
                if (logStringProvider.TryAsLoggable(obj, out loggable))
                    break;
            }
        }

        Type type = obj.GetType();
        loggable ??= new NonLoggable(type);

        if (isMaxDepth && loggable.IsDeep)
        {
            stringBuilder.Append(LogStringTokens.Deep);
            return;
        }

        try
        {
            using IDisposable? _3 = loggable.CanCycle ? AddSeen(obj) : null;
            loggable.AppendTo(stringBuilder, this);
        }
        catch (AlreadySeenShortCircuit)
        {
            stringBuilder
                .AppendLogString(type, this, false)
                .Append(LogStringTokens.Cycle);
        }
    }

    public IDisposable? AddSeen(object obj)
    {
        if (obj is ValueType)
        {
            return null;
        }
        if (renderedObjs.Add(obj))
        {
            return new CallbackDisposable(() => { renderedObjs.Remove(obj); });
        }
        throw new AlreadySeenShortCircuit();
    }

    private IDisposable? WithThresholdsSafe(Action<LogStringThresholdConfiguration>? configureThresholds)
    {
        return configureThresholds is null ? null : WithThresholds(configureThresholds);
    }

    public IDisposable WithThresholds(Action<LogStringThresholdConfiguration> configureThresholds)
    {
        LogStringThresholdConfiguration previous = thresholdConfiguration;
        LogStringThresholdConfiguration clone = new (previous);
        configureThresholds(clone);
        thresholdConfiguration = clone;
        return new CallbackDisposable(() => thresholdConfiguration = previous);
    }

    private IDisposable? WithMetaPropertiesSafe(Action<IDictionary<string, object?>>? configureMetaProperties)
    {
        return configureMetaProperties is null ? null : WithMetaProperties(configureMetaProperties);
    }

    public IDisposable WithMetaProperties(Action<IDictionary<string, object?>> configureMetaProperties)
    {
        Dictionary<string, object?> previous = metaProperties;
        Dictionary<string, object?> clone = new (previous, previous.Comparer);
        configureMetaProperties(clone);
        metaProperties = clone;
        return new CallbackDisposable(() => metaProperties = previous);
    }

    public IDisposable IncrementDepth(out bool isMaxDepth)
    {
        currentDepth += 1;
        isMaxDepth = currentDepth >= thresholdConfiguration.EffectiveMaxDepth;
        return new CallbackDisposable(() => currentDepth -= 1);
    }

    public AllottingCounter CountCollectionItems() => Count(thresholdConfiguration.EffectiveMaxCollectionItemCount);

    public AllottingCounter CountDictionaryItems() => Count(thresholdConfiguration.EffectiveMaxDictionaryItemCount);

    public AllottingCounter CountMemberwiseProperties() => Count(thresholdConfiguration.EffectiveMaxMemberwisePropertyCount);

    public AllottingCounter CountAnonymousObjectProperties() => Count(thresholdConfiguration.EffectiveMaxAnonymousObjectPropertyCount);

    public AllottingCounter CountTupleItems() => Count(thresholdConfiguration.EffectiveMaxTupleItemCount);

    public AllottingCounter CountMethodParameters() => Count(thresholdConfiguration.EffectiveMaxMethodParameterCount);

    private static AllottingCounter Count(int? maybeMax)
    {
        return maybeMax is { } max ? new LimitedAllottingCounter(max) : UnlimitedAllottingCounter.Instance;
    }

    private sealed class UnlimitedAllottingCounter : AllottingCounter
    {
        public static readonly AllottingCounter Instance = new UnlimitedAllottingCounter();

        private UnlimitedAllottingCounter() { }

        protected override bool TryDecrement() => true;
    }

    private sealed class LimitedAllottingCounter : AllottingCounter
    {
        private int current;

        public LimitedAllottingCounter(int max)
        {
            current = max;
        }

        protected override bool TryDecrement() => --current >= 0;
    }
}
