using System.Text;

namespace Diginsight.Strings;

public sealed class AppendingContext
{
    private readonly IEnumerable<ILogStringProvider> logStringProviders;
    private readonly ISet<object> renderedObjs = new HashSet<object>(ReferenceEqualityComparer.Instance);

    private LogStringVariableConfiguration variableConfiguration;
    private Dictionary<string, object?> metaProperties;
    private int currentDepth = 0;

    public ILogStringNamespaceConfiguration NamespaceConfiguration => variableConfiguration;

    public IReadOnlyDictionary<string, object?> MetaProperties => metaProperties;

    internal AppendingContext(
        IEnumerable<ILogStringProvider> logStringProviders,
        LogStringVariableConfiguration variableConfiguration,
        IEqualityComparer<string> metaPropertyKeyComparer
    )
    {
        this.logStringProviders = logStringProviders;
        this.variableConfiguration = variableConfiguration;
        metaProperties = new Dictionary<string, object?>(metaPropertyKeyComparer);
    }

    public void Append(
        object? obj,
        StringBuilder stringBuilder,
        bool incrementDepth = true,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        if (obj == null)
        {
            stringBuilder.Append('□');
            return;
        }

        using IDisposable? _0 = WithVariablesSafe(configureVariables);
        using IDisposable? _1 = WithMetaPropertiesSafe(configureMetaProperties);
        using IDisposable? _2 = this.IncrementDepth(incrementDepth, out bool isMaxDepth);

        ILogStringable? logStringable = obj as ILogStringable;
        if (logStringable is null)
        {
            foreach (ILogStringProvider logStringProvider in logStringProviders)
            {
                if ((logStringable = logStringProvider.TryAsLogStringable(obj)) is not null)
                    break;
            }
        }

        Type type = obj.GetType();
        logStringable ??= new NonLogStringable(type);

        if (isMaxDepth && logStringable.IsDeep)
        {
            stringBuilder.Append(LogStringTokens.Deep);
            return;
        }

        try
        {
            using IDisposable? _3 = logStringable.CanCycle ? AddSeen(obj) : null;
            logStringable.AppendTo(stringBuilder, this);
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

    public IDisposable? WithVariablesSafe(Action<LogStringVariableConfiguration>? configureVariables)
    {
        return configureVariables is null ? null : WithVariables(configureVariables);
    }

    public IDisposable WithVariables(Action<LogStringVariableConfiguration> configureVariables)
    {
        LogStringVariableConfiguration previous = variableConfiguration;
        LogStringVariableConfiguration clone = new (previous);
        configureVariables(clone);
        variableConfiguration = clone;
        return new CallbackDisposable(() => variableConfiguration = previous);
    }

    public IDisposable? WithMetaPropertiesSafe(Action<IDictionary<string, object?>>? configureMetaProperties)
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
        isMaxDepth = currentDepth >= variableConfiguration.EffectiveMaxDepth;
        return new CallbackDisposable(() => currentDepth -= 1);
    }

    public AllottingCounter CountCollectionItems() => Count(variableConfiguration.EffectiveMaxCollectionItemCount);

    public AllottingCounter CountDictionaryItems() => Count(variableConfiguration.EffectiveMaxDictionaryItemCount);

    public AllottingCounter CountMemberwiseProperties() => Count(variableConfiguration.EffectiveMaxMemberwisePropertyCount);

    public AllottingCounter CountAnonymousObjectProperties() => Count(variableConfiguration.EffectiveMaxAnonymousObjectPropertyCount);

    public AllottingCounter CountTupleItems() => Count(variableConfiguration.EffectiveMaxTupleItemCount);

    public AllottingCounter CountMethodParameters() => Count(variableConfiguration.EffectiveMaxMethodParameterCount);

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
