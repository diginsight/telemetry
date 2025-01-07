using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight.Stringify;

public sealed class StringifyContext
{
    private readonly StringBuilder stringBuilder;
    private readonly IEnumerable<IStringifier> stringifiers;
    private readonly IMemberInfoStringifier memberInfoStringifier;
    private readonly IDictionary<object, int> renderedObjs = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
    private readonly int maxTotalLength;

    private StringifyVariableConfiguration variableConfiguration;
    private Dictionary<string, object?> metaProperties;
    private Timer timer;
    private int currentDepth = 0;
    private bool isFull = false;

    public IStringifyVariableConfiguration VariableConfiguration => variableConfiguration;

    public IReadOnlyDictionary<string, object?> MetaProperties => metaProperties;

    public bool IsTimeOver => timer.IsOver;

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

    internal StringifyContext(
        StringBuilder stringBuilder,
        IEnumerable<IStringifier> stringifiers,
        IMemberInfoStringifier memberInfoStringifier,
        StringifyVariableConfiguration variableConfiguration,
        Expiration maxTime,
        int? maxTotalLength,
        IEqualityComparer<string> metaPropertyKeyComparer
    )
    {
        this.stringBuilder = stringBuilder;
        this.stringifiers = stringifiers;
        this.memberInfoStringifier = memberInfoStringifier;
        this.variableConfiguration = variableConfiguration;
        this.maxTotalLength = maxTotalLength ?? int.MaxValue;
        metaProperties = new Dictionary<string, object?>(metaPropertyKeyComparer);
        timer = new Timer(maxTime);
    }

    public StringifyContext ComposeAndAppend(
        object? obj,
        bool? atomic = null,
        Action<StringifyVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        if (atomic ?? true)
        {
            return AppendAtom(sc => sc.ComposeAndAppendCore(obj, configureVariables, configureMetaProperties));
        }
        else
        {
            ComposeAndAppendCore(obj, configureVariables, configureMetaProperties);
            return this;
        }
    }

    private void ComposeAndAppendCore(
        object? obj,
        Action<StringifyVariableConfiguration>? configureVariables,
        Action<IDictionary<string, object?>>? configureMetaProperties
    )
    {
        ComposeAndAppendCore(ToStringifiable(obj), configureVariables, configureMetaProperties);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IStringifiable ToStringifiable(object? obj) => StringifyContextFactory.ToStringifiable(obj, stringifiers);

    private void ComposeAndAppendCore(
        in IStringifiable stringifiable,
        Action<StringifyVariableConfiguration>? configureVariables,
        Action<IDictionary<string, object?>>? configureMetaProperties
    )
    {
        using IDisposable? _0 = this.WithVariablesSafe(configureVariables);
        using IDisposable? _1 = this.WithMetaPropertiesSafe(configureMetaProperties);
        using IDisposable? _2 = this.IncrementDepth(stringifiable.IsDeep, out bool isMaxDepth);

        if (isMaxDepth)
        {
            this.AppendDeep();
            return;
        }

        try
        {
            using IDisposable? _3 = AddSeen(stringifiable.Subject);
            try
            {
                stringifiable.AppendTo(this);
            }
            catch (Exception exception) when (exception is not ShortCircuit)
            {
                this.AppendError();
            }
        }
        catch (AlreadySeenShortCircuit shortCircuit)
        {
            ComposeAndAppendType(shortCircuit.Subject.GetType())
                .AppendDirect('~')
                .AppendDirect(shortCircuit.DepthDelta.ToStringInvariant());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringifyContext AppendDirect(char c)
    {
        if (!IsFull)
        {
            stringBuilder.Append(c);
        }

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringifyContext AppendDirect(string s)
    {
        if (IsFull)
            return this;

        stringBuilder.Append(s);
        ChopIfFull();
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringifyContext AppendDirect(Action<StringBuilder> appendContent)
    {
        if (IsFull)
            return this;

        appendContent(stringBuilder);
        ChopIfFull();
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringifyContext ComposeAndAppendType(Type type, object? collectionLength = null)
    {
        using IDisposable? _0 = WithDedicatedTime(Expiration.Never);
        using IDisposable? _1 = this.WithMetaPropertiesSafe(
            collectionLength is null
                ? null
                : x => { x[MemberInfoStringifier.CollectionLengthMetaProperty] = collectionLength; }
        );
        using IDisposable _2 = WithVariables(static x => { x.MaxDepth = Threshold.Unspecified; });

        memberInfoStringifier.Append(type, this);
        return this;
    }

    public StringifyContext AppendAtom(Action<StringifyContext> appendContent)
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

    public IDisposable? AddSeen(object? obj)
    {
        if (obj is null or ValueType)
            return null;

        if (renderedObjs.TryGetValue(obj, out int previousDepth))
            throw new AlreadySeenShortCircuit(obj, currentDepth - previousDepth);

        renderedObjs[obj] = currentDepth;
        return new CallbackDisposable(() => { renderedObjs.Remove(obj); });
    }

    public IDisposable WithVariables(Action<StringifyVariableConfiguration> configureVariables)
    {
        StringifyVariableConfiguration previous = variableConfiguration;
        StringifyVariableConfiguration clone = new (previous);
        configureVariables(clone);
        variableConfiguration = clone;
        return new CallbackDisposable(() => { variableConfiguration = previous; });
    }

    public IDisposable WithMetaProperties(Action<IDictionary<string, object?>> configureMetaProperties)
    {
        Dictionary<string, object?> previous = metaProperties;
        Dictionary<string, object?> clone = new (previous, previous.Comparer);
        configureMetaProperties(clone);
        metaProperties = clone;
        return new CallbackDisposable(() => { metaProperties = previous; });
    }

    public IDisposable? WithDedicatedTime(Expiration maxTime)
    {
        if (IsTimeOver)
            return null;

        Timer previousTimer = Interlocked.Exchange(ref timer, new Timer(maxTime));
        IDisposable? resume = previousTimer.Suspend();
        return new CallbackDisposable(
            () =>
            {
                resume?.Dispose();
                Interlocked.Exchange(ref timer, previousTimer);
            }
        );
    }

    public IDisposable IncrementDepth(out bool isMaxDepth)
    {
        currentDepth += 1;
        isMaxDepth = currentDepth > VariableConfiguration.GetEffectiveMaxDepth();
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

    private sealed class Timer
    {
        private readonly long maxTicks;
        private readonly Stopwatch? stopwatch;
        private bool isOver = false;

        public bool IsOver
        {
            get
            {
                if (isOver)
                    return true;
                if (stopwatch is null || stopwatch.ElapsedTicks <= maxTicks)
                    return false;
                stopwatch.Stop();
                return isOver = true;
            }
        }

        public Timer(Expiration expiration)
        {
            if (expiration.IsNever)
            {
                maxTicks = 0;
                stopwatch = null;
            }
            else
            {
                maxTicks = expiration.Value.Ticks;
                stopwatch = Stopwatch.StartNew();
            }
        }

        public IDisposable? Suspend()
        {
            if (stopwatch?.IsRunning != true)
                return null;

            stopwatch.Stop();
            return new CallbackDisposable(stopwatch.Start);
        }
    }
}
