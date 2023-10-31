using System.Text;
using Timer = System.Timers.Timer;

namespace Diginsight.Strings;

// FIXME AppendingContext
public sealed class AppendingContext
{
    private readonly Stack<StringBuilder> stringBuilders = new ();
    private readonly IEnumerable<ILogStringProvider> logStringProviders;
    private readonly ISet<object> renderedObjs = new HashSet<object>(ReferenceEqualityComparer.Instance);

    private StringBuilder stringBuilder;
    private LogStringVariableConfiguration variableConfiguration;
    private Dictionary<string, object?> metaProperties;
    private int currentDepth = 0;
    //private bool isAtomic = false;
    //private bool isTimeOver = false;

    public ILogStringVariableConfiguration VariableConfiguration => variableConfiguration;

    public IReadOnlyDictionary<string, object?> MetaProperties => metaProperties;

    public bool IsTimeOver { get; private set; } //=> isTimeOver && !isAtomic;

    internal AppendingContext(
        StringBuilder stringBuilder,
        IEnumerable<ILogStringProvider> logStringProviders,
        LogStringVariableConfiguration variableConfiguration,
        TimeSpan maxTime,
        IEqualityComparer<string> metaPropertyKeyComparer
    )
    {
        stringBuilders.Push(this.stringBuilder = stringBuilder);

        this.logStringProviders = logStringProviders;
        this.variableConfiguration = variableConfiguration;
        metaProperties = new Dictionary<string, object?>(metaPropertyKeyComparer);

        if (maxTime > TimeSpan.Zero)
        {
            Timer timer = new (maxTime.TotalMilliseconds);
            timer.Elapsed += (_, _) => IsTimeOver = true;
            timer.Start();
        }
    }

    public AppendingContext ComposeAndAppend(
        object? obj,
        bool incrementDepth = true,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        if (IsTimeOver)
        {
            AppendPunctuation(LogStringTokens.Ellipsis);
            return this;
        }

        if (obj == null)
        {
            AppendPunctuation('□');
            return this;
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
            AppendPunctuation(LogStringTokens.Deep);
            return this;
        }

        try
        {
            using IDisposable? _3 = logStringable.CanCycle ? AddSeen(obj) : null;
            logStringable.AppendTo(this);
        }
        catch (AlreadySeenShortCircuit)
        {
            ComposeAndAppend(type, false)
                .AppendPunctuation(LogStringTokens.Cycle);
        }

        return this;
    }

    public AppendingContext AppendDirect(Action<StringBuilder> appendContent)
    {
        ThrowIfTimeIsOver();
        appendContent(stringBuilder);
        return this;
    }

    public AppendingContext AppendPunctuation(char c)
    {
        stringBuilder.Append(c);
        return this;
    }

    public AppendingContext AppendPunctuation(string s)
    {
        stringBuilder.Append(s);
        return this;
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

    //public IDisposable WithAtomic()
    //{
    //    bool prevIsAtomic = isAtomic;
    //    isAtomic = true;
    //    return new CallbackDisposable(() => { isAtomic = prevIsAtomic; });
    //}

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
