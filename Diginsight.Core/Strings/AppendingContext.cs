using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight.Strings;

public sealed class AppendingContext
{
    private readonly StringBuilder stringBuilder;
    private readonly IEnumerable<ILogStringProvider> logStringProviders;
    private readonly IMemberInfoLogStringProvider memberInfoLogStringProvider;
    private readonly ISet<object> renderedObjs = new HashSet<object>(ReferenceEqualityComparer.Instance);
    private readonly long maxTimeTicks;
    private readonly int maxTotalLength;
    private readonly Stopwatch? stopwatch;

    private LogStringVariableConfiguration variableConfiguration;
    private Dictionary<string, object?> metaProperties;
    private int currentDepth = 0;
    private bool isTimeOver = false;
    private bool isFull = false;

    public ILogStringVariableConfiguration VariableConfiguration => variableConfiguration;

    public IReadOnlyDictionary<string, object?> MetaProperties => metaProperties;

    public bool IsTimeOver
    {
        get
        {
            if (isTimeOver)
                return true;
            if (stopwatch is null || stopwatch.ElapsedTicks <= maxTimeTicks)
                return false;
            stopwatch.Stop();
            return isTimeOver = true;
        }
    }

    public bool IsFull
    {
        get
        {
            if (isFull)
                return true;
            if (stringBuilder.Length < maxTotalLength)
                return false;
            return isFull = true;
        }
    }

    internal AppendingContext(
        StringBuilder stringBuilder,
        IEnumerable<ILogStringProvider> logStringProviders,
        IMemberInfoLogStringProvider memberInfoLogStringProvider,
        LogStringVariableConfiguration variableConfiguration,
        TimeSpan maxTime,
        int? maxTotalLength,
        IEqualityComparer<string> metaPropertyKeyComparer
    )
    {
        this.stringBuilder = stringBuilder;
        this.logStringProviders = logStringProviders;
        this.memberInfoLogStringProvider = memberInfoLogStringProvider;
        this.variableConfiguration = variableConfiguration;
        this.maxTotalLength = maxTotalLength ?? int.MaxValue;
        metaProperties = new Dictionary<string, object?>(metaPropertyKeyComparer);

        if (maxTime > TimeSpan.Zero)
        {
            maxTimeTicks = maxTime.Ticks;
            stopwatch = Stopwatch.StartNew();
        }
        else
        {
            maxTimeTicks = 0;
            stopwatch = null;
        }
    }

    public AppendingContext ComposeAndAppend(
        object? obj,
        bool incrementDepth = true,
        bool? atomic = null,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        if (atomic ?? incrementDepth)
        {
            return AppendAtom(ac => ac.ComposeAndAppendCore(obj, incrementDepth, configureVariables, configureMetaProperties));
        }
        else
        {
            ComposeAndAppendCore(obj, incrementDepth, configureVariables, configureMetaProperties);
            return this;
        }
    }

    private void ComposeAndAppendCore(
        object? obj,
        bool incrementDepth,
        Action<LogStringVariableConfiguration>? configureVariables,
        Action<IDictionary<string, object?>>? configureMetaProperties
    )
    {
        if (obj == null)
        {
            AppendDirect('□');
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
            this.AppendDeep();
            return;
        }

        try
        {
            using IDisposable? _3 = logStringable.CanCycle ? AddSeen(obj) : null;
            logStringable.AppendTo(this);
        }
        catch (AlreadySeenShortCircuit)
        {
            ComposeAndAppendType(type).AppendDirect(LogStringTokens.Cycle);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AppendingContext AppendDirect(char c)
    {
        if (!IsFull)
        {
            stringBuilder.Append(c);
        }

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AppendingContext AppendDirect(string s)
    {
        if (IsFull)
            return this;

        stringBuilder.Append(s);
        ChopIfFull();
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AppendingContext AppendDirect(Action<StringBuilder> appendContent)
    {
        if (IsFull)
            return this;

        appendContent(stringBuilder);
        ChopIfFull();
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AppendingContext ComposeAndAppendType(Type type, object? collectionLength = null)
    {
        using IDisposable? _0 = WithMetaPropertiesSafe(
            configureMetaProperties: collectionLength is null
                ? null
                : x => { x[MemberInfoLogStringProvider.CollectionLengthMetaProperty] = collectionLength; }
        );

        memberInfoLogStringProvider.Append(type, this);
        return this;
    }

    public AppendingContext AppendAtom(Action<AppendingContext> appendContent)
    {
        int prevLength = stringBuilder.Length;
        try
        {
            ThrowIfTimeIsOver();
            appendContent(this);
        }
        catch (MaxAllottedTimeShortCircuit)
        {
            stringBuilder.Remove(prevLength, stringBuilder.Length - prevLength);
            this.AppendEllipsis();
        }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ThrowIfTimeIsOver()
    {
        if (IsTimeOver)
        {
            throw new MaxAllottedTimeShortCircuit();
        }
    }

    public void ChopIfFull()
    {
        int excessLength = stringBuilder.Length - maxTotalLength;
        if (excessLength <= 0)
            return;

        stringBuilder.Remove(maxTotalLength, excessLength);
        isFull = true;
    }
}
