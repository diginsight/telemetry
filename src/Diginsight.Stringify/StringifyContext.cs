using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight.Stringify;

/// <summary>
/// Represents the mutable state used while composing a compact string representation.
/// </summary>
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

    /// <summary>
    /// Gets the active variable configuration.
    /// </summary>
    public IStringifyVariableConfiguration VariableConfiguration => variableConfiguration;

    /// <summary>
    /// Gets the active meta properties.
    /// </summary>
    public IReadOnlyDictionary<string, object?> MetaProperties => metaProperties;

    /// <summary>
    /// Gets a value indicating whether the allotted stringification time is over.
    /// </summary>
    public bool IsTimeOver => timer.IsOver;

    /// <summary>
    /// Gets a value indicating whether the output has reached the maximum total length.
    /// </summary>
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

    /// <summary>
    /// Composes and appends a stringifiable representation of the specified object.
    /// </summary>
    /// <param name="obj">The object to stringify.</param>
    /// <param name="atomic">
    /// A value indicating whether the value is appended atomically, as described by <see cref="AppendAtom" />.
    /// When <c>null</c>, an atomic append is used by default unless overridden by an <see cref="IStringifyModifier" />.
    /// </param>
    /// <param name="configureVariables">The action used to configure variable options.</param>
    /// <param name="configureMetaProperties">The action used to configure meta properties.</param>
    /// <param name="maxTime">The maximum allotted stringification time.</param>
    /// <returns>The same <see cref="StringifyContext" /> instance, for chaining.</returns>
    public StringifyContext ComposeAndAppend(
        object? obj,
        bool? atomic = null,
        Action<StringifyVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null,
        Expiration? maxTime = null
    )
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Action<T>? Combine<T>(Action<T>? a1, Action<T>? a2)
        {
            return a1 is null ? a2
                : a2 is null ? a1
                : x =>
                {
                    a1(x);
                    a2(x);
                };
        }

        if (obj is IStringifyModifier modifier)
        {
            return ComposeAndAppend(
                modifier.Subject,
                atomic ?? modifier.Atomic,
                Combine(modifier.ConfigureVariables, configureVariables),
                Combine(modifier.ConfigureMetaProperties, configureMetaProperties),
                maxTime ?? modifier.MaxTime
            );
        }

        if (atomic ?? true)
        {
            return AppendAtom(sc => sc.ComposeAndAppendCore(obj, configureVariables, configureMetaProperties, maxTime));
        }
        else
        {
            ComposeAndAppendCore(obj, configureVariables, configureMetaProperties, maxTime);
            return this;
        }
    }

    private void ComposeAndAppendCore(
        object? obj,
        Action<StringifyVariableConfiguration>? configureVariables,
        Action<IDictionary<string, object?>>? configureMetaProperties,
        Expiration? maxTime
    )
    {
        ComposeAndAppendCore(ToStringifiable(obj), configureVariables, configureMetaProperties, maxTime);
    }

    /// <summary>
    /// Converts the specified object to a stringifiable representation.
    /// </summary>
    /// <param name="obj">The object to stringify.</param>
    /// <returns>The stringifiable representation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IStringifiable ToStringifiable(object? obj) => StringifyContextFactory.ToStringifiable(obj, stringifiers);

    private void ComposeAndAppendCore(
        in IStringifiable stringifiable,
        Action<StringifyVariableConfiguration>? configureVariables,
        Action<IDictionary<string, object?>>? configureMetaProperties,
        Expiration? maxTime
    )
    {
        using IDisposable? _0 = maxTime is not null ? WithDedicatedTime(maxTime.Value) : null;
        using IDisposable? _1 = this.WithVariablesSafe(configureVariables);
        using IDisposable? _2 = this.WithMetaPropertiesSafe(configureMetaProperties);
        using IDisposable? _3 = this.IncrementDepth(stringifiable.IsDeep, out bool isMaxDepth);

        if (isMaxDepth)
        {
            this.AppendDeep();
            return;
        }

        try
        {
            using IDisposable? _4 = AddSeen(stringifiable.Subject);
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

    /// <summary>
    /// Appends content directly to the underlying output.
    /// </summary>
    /// <param name="c">The character to append.</param>
    /// <returns>The stringify context.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringifyContext AppendDirect(char c)
    {
        if (!IsFull)
        {
            stringBuilder.Append(c);
        }

        return this;
    }

    /// <summary>
    /// Appends content directly to the underlying output.
    /// </summary>
    /// <param name="s">The string to append.</param>
    /// <returns>The stringify context.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringifyContext AppendDirect(string s)
    {
        if (IsFull)
            return this;

        stringBuilder.Append(s);
        ChopIfFull();
        return this;
    }

    /// <summary>
    /// Appends content directly to the underlying output.
    /// </summary>
    /// <param name="appendContent">The action that appends content.</param>
    /// <returns>The stringify context.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringifyContext AppendDirect(Action<StringBuilder> appendContent)
    {
        if (IsFull)
            return this;

        appendContent(stringBuilder);
        ChopIfFull();
        return this;
    }

    /// <summary>
    /// Composes and appends a compact representation of a type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="collectionLength">The collection length metadata to append after the type name.</param>
    /// <returns>The same <see cref="StringifyContext" /> instance, for chaining.</returns>
    /// <remarks>
    /// The <paramref name="collectionLength" /> value is applicable only to array and collection types and is rendered right after the type name.
    /// An <c>int[]</c> value is interpreted as the lengths of the array dimensions and is rendered inside the square brackets (for example, <c>[3]</c> or <c>[3,4]</c>).
    /// A single <c>int</c> value is rendered as a parenthesized suffix (for example, <c>(3)</c>).
    /// A <c>null</c> value appends no length information.
    /// The metadata applies to the outermost type only and is not propagated to nested type arguments or element types.
    /// </remarks>
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

    /// <summary>
    /// Appends content as an atomic operation.
    /// </summary>
    /// <param name="appendContent">The action that appends content.</param>
    /// <returns>The same <see cref="StringifyContext" /> instance, for chaining.</returns>
    /// <remarks>
    /// An atomic append is all-or-nothing with respect to the time budget: if the allotted stringification time expires while <paramref name="appendContent" />
    /// is running, any content it has already written is rolled back and replaced with an ellipsis, so an atomic block never leaves a truncated fragment in the output.
    /// Content appended outside an atomic block is not subject to this rollback.
    /// </remarks>
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

    /// <summary>
    /// Adds a subject to the set of objects currently being rendered.
    /// </summary>
    /// <param name="obj">The object to stringify.</param>
    /// <returns>A disposable that removes the subject from the seen set when disposed, or <c>null</c> for subjects that are not tracked.</returns>
    public IDisposable? AddSeen(object? obj)
    {
        if (obj is null or ValueType)
            return null;

        if (renderedObjs.TryGetValue(obj, out int previousDepth))
            throw new AlreadySeenShortCircuit(obj, currentDepth - previousDepth);

        renderedObjs[obj] = currentDepth;
        return new CallbackDisposable(() => { renderedObjs.Remove(obj); });
    }

    /// <summary>
    /// Applies temporary variable configuration to the stringify context.
    /// </summary>
    /// <param name="configureVariables">The action used to configure variable options.</param>
    /// <returns>A disposable that restores the previous variable configuration when disposed.</returns>
    public IDisposable WithVariables(Action<StringifyVariableConfiguration> configureVariables)
    {
        StringifyVariableConfiguration previous = variableConfiguration;
        StringifyVariableConfiguration clone = new (previous);
        configureVariables(clone);
        variableConfiguration = clone;
        return new CallbackDisposable(() => { variableConfiguration = previous; });
    }

    /// <summary>
    /// Applies temporary meta properties to the stringify context.
    /// </summary>
    /// <param name="configureMetaProperties">The action used to configure meta properties.</param>
    /// <returns>A disposable that restores the previous meta properties when disposed.</returns>
    public IDisposable WithMetaProperties(Action<IDictionary<string, object?>> configureMetaProperties)
    {
        Dictionary<string, object?> previous = metaProperties;
        Dictionary<string, object?> clone = new (previous, previous.Comparer);
        configureMetaProperties(clone);
        metaProperties = clone;
        return new CallbackDisposable(() => { metaProperties = previous; });
    }

    /// <summary>
    /// Applies a dedicated time budget to the stringify context.
    /// </summary>
    /// <param name="maxTime">The maximum allotted stringification time.</param>
    /// <returns>A disposable that restores the previous timer when disposed, or <c>null</c> if time is already over.</returns>
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

    /// <summary>
    /// Increments the current stringification depth.
    /// </summary>
    /// <param name="isMaxDepth">When this method returns, contains a value indicating whether the maximum depth was reached.</param>
    /// <returns>A disposable that restores the previous depth when disposed.</returns>
    public IDisposable IncrementDepth(out bool isMaxDepth)
    {
        currentDepth += 1;
        isMaxDepth = currentDepth > VariableConfiguration.EffectiveMaxDepth;
        return new CallbackDisposable(() => currentDepth -= 1);
    }

    /// <summary>
    /// Throws when the allotted stringification time is over.
    /// </summary>
    /// <exception cref="MaxAllottedTimeShortCircuit">Thrown when the allotted stringification time is over.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ThrowIfTimeIsOver()
    {
        if (IsTimeOver)
        {
            throw new MaxAllottedTimeShortCircuit();
        }
    }

    /// <summary>
    /// Truncates output that exceeds the maximum total length.
    /// </summary>
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

        public bool IsOver
        {
            get
            {
                if (field)
                    return true;
                if (stopwatch is null || stopwatch.ElapsedTicks <= maxTicks)
                    return false;
                stopwatch.Stop();
                return field = true;
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
