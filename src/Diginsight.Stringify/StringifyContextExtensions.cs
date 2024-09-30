using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Diginsight.Stringify;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class StringifyContextExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringifyContext AppendEllipsis(this StringifyContext stringifyContext)
    {
        return stringifyContext.AppendDirect(StringifyTokens.Ellipsis);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringifyContext AppendDeep(this StringifyContext stringifyContext)
    {
        return stringifyContext.AppendDirect(StringifyTokens.Deep);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringifyContext AppendError(this StringifyContext stringifyContext)
    {
        return stringifyContext.AppendDirect(StringifyTokens.Error);
    }

    public static StringifyContext AppendEnumerator<T>(
        this StringifyContext stringifyContext,
        T enumerator,
        Action<StringifyContext, T> appendCurrent,
        AllottedCounter counter,
        string separator = StringifyTokens.Separator2
    )
        where T : IEnumerator
    {
        try
        {
            bool first = true;
            bool over = false;

            bool? MoveNext()
            {
                try
                {
                    return enumerator.MoveNext();
                }
                catch (Exception)
                {
                    return null;
                }
            }

            void AppendSeparator()
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    stringifyContext.AppendDirect(separator);
                }
            }

            while (!over)
            {
                switch (MoveNext())
                {
                    case null:
                        AppendSeparator();
                        stringifyContext.AppendError();
                        over = true;
                        break;

                    case false:
                        over = true;
                        break;

                    case true:
                        AppendSeparator();
                        counter.Decrement();
                        stringifyContext.ThrowIfTimeIsOver();
                        appendCurrent(stringifyContext, enumerator);
                        break;
                }
            }
        }
        catch (MaxAllottedShortCircuit)
        {
            stringifyContext.AppendEllipsis();
        }

        return stringifyContext;
    }

    public static StringifyContext AppendDelimited(
        this StringifyContext stringifyContext,
        char beginDelim,
        char endDelim,
        Action<StringifyContext> appendContent,
        Action<StringifyVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        stringifyContext.AppendDirect(beginDelim);

        using (stringifyContext.WithVariablesSafe(configureVariables))
        using (stringifyContext.WithMetaPropertiesSafe(configureMetaProperties))
        {
            appendContent(stringifyContext);
        }

        return stringifyContext.AppendDirect(endDelim);
    }

    public static StringifyContext AppendMap(
        this StringifyContext stringifyContext,
        Type mapType,
        Action<StringifyContext> appendContent,
        Action<StringifyVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        return stringifyContext
            .ComposeAndAppendType(mapType)
            .AppendDelimited(
                StringifyTokens.MapBegin,
                StringifyTokens.MapEnd,
                appendContent,
                configureVariables,
                configureMetaProperties
            );
    }

    public static StringifyContext AppendCollection(
        this StringifyContext stringifyContext,
        Type collectionType,
        Action<StringifyContext> appendContent,
        int? count = null,
        Action<StringifyVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        return stringifyContext
            .ComposeAndAppendType(collectionType, count)
            .AppendDelimited(
                StringifyTokens.CollectionBegin,
                StringifyTokens.CollectionEnd,
                appendContent,
                configureVariables,
                configureMetaProperties
            );
    }

    public static MemberAppender ComposeAndAppendMember(
        this StringifyContext stringifyContext,
        string memberName,
        object? memberValue,
        string separator = StringifyTokens.Separator2,
        bool? atomic = null,
        Action<StringifyVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        AllottedCounter counter = stringifyContext.CountMemberwiseProperties();

        bool isAlive;
        try
        {
            counter.Decrement();
            stringifyContext.ThrowIfTimeIsOver();
            isAlive = true;

            stringifyContext
                .AppendDirect(memberName)
                .AppendDirect(StringifyTokens.Value)
                .ComposeAndAppend(memberValue, atomic, configureVariables, configureMetaProperties);
        }
        catch (MaxAllottedShortCircuit)
        {
            stringifyContext.AppendEllipsis();
            isAlive = false;
        }

        return new MemberAppender(stringifyContext, counter, separator, isAlive);
    }

    public static ItemAppender ComposeAndAppendItem(
        this StringifyContext stringifyContext,
        object? itemValue,
        string separator = StringifyTokens.Separator2,
        bool? atomic = null,
        Action<StringifyVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        AllottedCounter counter = stringifyContext.CountCollectionItems();

        bool isAlive;
        try
        {
            counter.Decrement();
            stringifyContext.ThrowIfTimeIsOver();
            isAlive = true;

            stringifyContext
                .ComposeAndAppend(itemValue, atomic, configureVariables, configureMetaProperties);
        }
        catch (MaxAllottedShortCircuit)
        {
            stringifyContext.AppendEllipsis();
            isAlive = false;
        }

        return new ItemAppender(stringifyContext, counter, separator, isAlive);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(configureVariables))]
    public static IDisposable? WithVariablesSafe(this StringifyContext stringifyContext, Action<StringifyVariableConfiguration>? configureVariables)
    {
        return configureVariables is null ? null : stringifyContext.WithVariables(configureVariables);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(configureMetaProperties))]
    public static IDisposable? WithMetaPropertiesSafe(this StringifyContext stringifyContext, Action<IDictionary<string, object?>>? configureMetaProperties)
    {
        return configureMetaProperties is null ? null : stringifyContext.WithMetaProperties(configureMetaProperties);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IDisposable? IncrementDepth(this StringifyContext stringifyContext, bool condition, out bool isMaxDepth)
    {
        if (condition)
        {
            return stringifyContext.IncrementDepth(out isMaxDepth);
        }
        else
        {
            isMaxDepth = false;
            return null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AllottedCounter CountCollectionItems(this StringifyContext stringifyContext)
    {
        return AllottedCounter.Count(stringifyContext.VariableConfiguration.GetEffectiveMaxCollectionItemCount());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AllottedCounter CountDictionaryItems(this StringifyContext stringifyContext)
    {
        return AllottedCounter.Count(stringifyContext.VariableConfiguration.GetEffectiveMaxDictionaryItemCount());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AllottedCounter CountMemberwiseProperties(this StringifyContext stringifyContext)
    {
        return AllottedCounter.Count(stringifyContext.VariableConfiguration.GetEffectiveMaxMemberwisePropertyCount());
    }
}
