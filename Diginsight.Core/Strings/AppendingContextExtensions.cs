using System.Collections;
using System.Runtime.CompilerServices;

namespace Diginsight.Strings;

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

    public static AppendingContext AppendEnumerator<T>(
        this AppendingContext appendingContext,
        T enumerator,
        Action<AppendingContext, T> appendCurrent,
        AllottingCounter counter,
        string separator = LogStringTokens.Separator2
    )
        where T : IEnumerator
    {
        if (!enumerator.MoveNext())
        {
            return appendingContext;
        }

        try
        {
            void AppendEntry()
            {
                counter.Decrement();
                appendingContext.ThrowIfTimeIsOver();

                appendCurrent(appendingContext, enumerator);
            }

            AppendEntry();
            while (enumerator.MoveNext())
            {
                appendingContext.AppendDirect(separator);
                AppendEntry();
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
        bool incrementDepth = true,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        appendingContext.AppendDirect(beginDelim);

        using (appendingContext.WithVariablesSafe(configureVariables))
        using (appendingContext.WithMetaPropertiesSafe(configureMetaProperties))
        using (appendingContext.IncrementDepth(incrementDepth, out bool isMaxDepth))
        {
            if (isMaxDepth)
            {
                appendingContext.AppendDeep();
            }
            else
            {
                appendContent(appendingContext);
            }
        }

        return appendingContext.AppendDirect(endDelim);
    }

    public static AppendingContext AppendMap(
        this AppendingContext appendingContext,
        Type mapType,
        Action<AppendingContext> appendContent,
        bool incrementDepth = true,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        return appendingContext
            .ComposeAndAppend(mapType, false)
            .AppendDelimited(
                LogStringTokens.MapBegin,
                LogStringTokens.MapEnd,
                appendContent,
                incrementDepth,
                configureVariables,
                configureMetaProperties
            );
    }

    public static AppendingContext AppendCollection(
        this AppendingContext appendingContext,
        Type collectionType,
        Action<AppendingContext> appendContent,
        int? count = null,
        bool incrementDepth = true,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        return appendingContext
            .ComposeAndAppend(
                collectionType,
                false,
                configureMetaProperties: x => { x[MemberInfoLogStringProvider.CollectionLengthMetaProperty] = count; }
            )
            .AppendDelimited(
                LogStringTokens.CollectionBegin,
                LogStringTokens.CollectionEnd,
                appendContent,
                incrementDepth,
                configureVariables,
                configureMetaProperties
            );
    }

    public static MemberAppender ComposeAndAppendMember(
        this AppendingContext appendingContext,
        string memberName,
        object? memberValue,
        string separator = LogStringTokens.Separator2,
        bool incrementDepth = true,
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
                .ComposeAndAppend(memberValue, incrementDepth, atomic, configureVariables, configureMetaProperties);
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
        bool incrementDepth = true,
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
                .ComposeAndAppend(itemValue, incrementDepth, atomic, configureVariables, configureMetaProperties);
        }
        catch (MaxAllottedShortCircuit)
        {
            appendingContext.AppendEllipsis();
            isAlive = false;
        }

        return new ItemAppender(appendingContext, counter, separator, isAlive);
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
