using System.Text;
using Timer = System.Timers.Timer;

namespace Diginsight.Strings;

public sealed class AppendingContext
{
    private readonly IEnumerable<ILogStringProvider> logStringProviders;
    private readonly ISet<object> renderedObjs = new HashSet<object>(ReferenceEqualityComparer.Instance);

    private LogStringVariableConfiguration variableConfiguration;
    private Dictionary<string, object?> metaProperties;
    private int currentDepth = 0;
    private bool isAtomic = false;
    private bool isTimeOver = false;

    public ILogStringVariableConfiguration VariableConfiguration => variableConfiguration;

    public IReadOnlyDictionary<string, object?> MetaProperties => metaProperties;

    public bool IsTimeOver => isTimeOver && !isAtomic;

    internal AppendingContext(
        IEnumerable<ILogStringProvider> logStringProviders,
        LogStringVariableConfiguration variableConfiguration,
        TimeSpan maxTime,
        IEqualityComparer<string> metaPropertyKeyComparer
    )
    {
        this.logStringProviders = logStringProviders;
        this.variableConfiguration = variableConfiguration;
        metaProperties = new Dictionary<string, object?>(metaPropertyKeyComparer);

        if (maxTime > TimeSpan.Zero)
        {
            Timer timer = new (maxTime.TotalMilliseconds);
            timer.Elapsed += (_, _) => isTimeOver = true;
            timer.Start();
        }
    }

    public void ComposeAndAppend(
        object? obj,
        StringBuilder stringBuilder,
        bool incrementDepth = true,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        if (IsTimeOver)
        {
            stringBuilder.Append(LogStringTokens.Ellipsis);
            return;
        }

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
                .ComposeAndAppend(type, this, false)
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

    public IDisposable WithAtomic()
    {
        bool prevIsAtomic = isAtomic;
        isAtomic = true;
        return new CallbackDisposable(() => { isAtomic = prevIsAtomic; });
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
        return new CallbackDisposable(() => { variableConfiguration = previous; });
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
        return new CallbackDisposable(() => { metaProperties = previous; });
    }

    public IDisposable IncrementDepth(out bool isMaxDepth)
    {
        currentDepth += 1;
        isMaxDepth = currentDepth >= VariableConfiguration.GetEffectiveMaxDepth();
        return new CallbackDisposable(() => currentDepth -= 1);
    }

    public void ThrowIfTimeIsOver()
    {
        if (IsTimeOver)
        {
            throw new MaxAllottedTimeShortCircuit();
        }
    }
}
