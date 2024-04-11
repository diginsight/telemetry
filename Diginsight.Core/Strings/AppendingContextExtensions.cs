using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Diginsight.Strings;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class AppendingContextExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AppendingContext AppendEllipsis(this AppendingContext appendingContext)
    {
        return appendingContext.AppendDirect(LogStringTokens.Ellipsis);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AppendingContext AppendDeep(this AppendingContext appendingContext)
    {
        return appendingContext.AppendDirect(LogStringTokens.Deep);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AppendingContext AppendError(this AppendingContext appendingContext)
    {
        return appendingContext.AppendDirect(LogStringTokens.Error);
    }

    public static AppendingContext AppendEnumerator<T>(
        this AppendingContext appendingContext,
        T enumerator,
        Action<AppendingContext, T> appendCurrent,
        AllottingCounter counter,
        string separator = LogStringTokens.Separator2
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
                    appendingContext.AppendDirect(separator);
                }
            }

            while (!over)
            {
                switch (MoveNext())
                {
                    case null:
                        AppendSeparator();
                        appendingContext.AppendError();
                        over = true;
                        break;

                    case false:
                        over = true;
                        break;

                    case true:
                        AppendSeparator();
                        counter.Decrement();
                        appendingContext.ThrowIfTimeIsOver();
                        appendCurrent(appendingContext, enumerator);
                        break;
                }
            }
        }
        catch (MaxAllottedShortCircuit)
        {
            appendingContext.AppendEllipsis();
        }

        return appendingContext;
    }

    public static AppendingContext AppendDelimited(
        this AppendingContext appendingContext,
        char beginDelim,
        char endDelim,
        Action<AppendingContext> appendContent,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        appendingContext.AppendDirect(beginDelim);

        using (appendingContext.WithVariablesSafe(configureVariables))
        using (appendingContext.WithMetaPropertiesSafe(configureMetaProperties))
        {
            appendContent(appendingContext);
        }

        return appendingContext.AppendDirect(endDelim);
    }

    public static AppendingContext AppendMap(
        this AppendingContext appendingContext,
        Type mapType,
        Action<AppendingContext> appendContent,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        return appendingContext
            .ComposeAndAppendType(mapType)
            .AppendDelimited(
                LogStringTokens.MapBegin,
                LogStringTokens.MapEnd,
                appendContent,
                configureVariables,
                configureMetaProperties
            );
    }

    public static AppendingContext AppendCollection(
        this AppendingContext appendingContext,
        Type collectionType,
        Action<AppendingContext> appendContent,
        int? count = null,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        return appendingContext
            .ComposeAndAppendType(collectionType, count)
            .AppendDelimited(
                LogStringTokens.CollectionBegin,
                LogStringTokens.CollectionEnd,
                appendContent,
                configureVariables,
                configureMetaProperties
            );
    }

    public static MemberAppender ComposeAndAppendMember(
        this AppendingContext appendingContext,
        string memberName,
        object? memberValue,
        string separator = LogStringTokens.Separator2,
        bool? atomic = null,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        AllottingCounter counter = appendingContext.CountMemberwiseProperties();

        bool isAlive;
        try
        {
            counter.Decrement();
            appendingContext.ThrowIfTimeIsOver();
            isAlive = true;

            appendingContext
                .AppendDirect(memberName)
                .AppendDirect(LogStringTokens.Value)
                .ComposeAndAppend(memberValue, atomic, configureVariables, configureMetaProperties);
        }
        catch (MaxAllottedShortCircuit)
        {
            appendingContext.AppendEllipsis();
            isAlive = false;
        }

        return new MemberAppender(appendingContext, counter, separator, isAlive);
    }

    public static ItemAppender ComposeAndAppendItem(
        this AppendingContext appendingContext,
        object? itemValue,
        string separator = LogStringTokens.Separator2,
        bool? atomic = null,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        AllottingCounter counter = appendingContext.CountCollectionItems();

        bool isAlive;
        try
        {
            counter.Decrement();
            appendingContext.ThrowIfTimeIsOver();
            isAlive = true;

            appendingContext
                .ComposeAndAppend(itemValue, atomic, configureVariables, configureMetaProperties);
        }
        catch (MaxAllottedShortCircuit)
        {
            appendingContext.AppendEllipsis();
            isAlive = false;
        }

        return new ItemAppender(appendingContext, counter, separator, isAlive);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(configureVariables))]
    public static IDisposable? WithVariablesSafe(this AppendingContext appendingContext, Action<LogStringVariableConfiguration>? configureVariables)
    {
        return configureVariables is null ? null : appendingContext.WithVariables(configureVariables);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(configureMetaProperties))]
    public static IDisposable? WithMetaPropertiesSafe(this AppendingContext appendingContext, Action<IDictionary<string, object?>>? configureMetaProperties)
    {
        return configureMetaProperties is null ? null : appendingContext.WithMetaProperties(configureMetaProperties);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IDisposable? IncrementDepth(this AppendingContext appendingContext, bool condition, out bool isMaxDepth)
    {
        if (condition)
        {
            return appendingContext.IncrementDepth(out isMaxDepth);
        }
        else
        {
            isMaxDepth = false;
            return null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AllottingCounter CountCollectionItems(this AppendingContext appendingContext)
    {
        return AllottingCounter.Count(appendingContext.VariableConfiguration.GetEffectiveMaxCollectionItemCount());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AllottingCounter CountDictionaryItems(this AppendingContext appendingContext)
    {
        return AllottingCounter.Count(appendingContext.VariableConfiguration.GetEffectiveMaxDictionaryItemCount());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AllottingCounter CountMemberwiseProperties(this AppendingContext appendingContext)
    {
        return AllottingCounter.Count(appendingContext.VariableConfiguration.GetEffectiveMaxMemberwisePropertyCount());
    }
}
