using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Diginsight.Stringify;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class StringifyContextExtensions
{
    extension(StringifyContext stringifyContext)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringifyContext AppendEllipsis()
        {
            return stringifyContext.AppendDirect(StringifyTokens.Ellipsis);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringifyContext AppendDeep()
        {
            return stringifyContext.AppendDirect(StringifyTokens.Deep);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringifyContext AppendError()
        {
            return stringifyContext.AppendDirect(StringifyTokens.Error);
        }

        public StringifyContext AppendEnumerator<T>(
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

        public StringifyContext AppendDelimited(
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

        public StringifyContext AppendMap(
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

        public StringifyContext AppendCollection(
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

        public MemberAppender ComposeAndAppendMember(
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

        public ItemAppender ComposeAndAppendItem(
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
        public IDisposable? WithVariablesSafe(Action<StringifyVariableConfiguration>? configureVariables)
        {
            return configureVariables is null ? null : stringifyContext.WithVariables(configureVariables);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNullIfNotNull(nameof(configureMetaProperties))]
        public IDisposable? WithMetaPropertiesSafe(Action<IDictionary<string, object?>>? configureMetaProperties)
        {
            return configureMetaProperties is null ? null : stringifyContext.WithMetaProperties(configureMetaProperties);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDisposable? IncrementDepth(bool condition, out bool isMaxDepth)
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
        public AllottedCounter CountCollectionItems()
        {
            return AllottedCounter.Count(stringifyContext.VariableConfiguration.GetEffectiveMaxCollectionItemCount());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AllottedCounter CountDictionaryItems()
        {
            return AllottedCounter.Count(stringifyContext.VariableConfiguration.GetEffectiveMaxDictionaryItemCount());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AllottedCounter CountMemberwiseProperties()
        {
            return AllottedCounter.Count(stringifyContext.VariableConfiguration.GetEffectiveMaxMemberwisePropertyCount());
        }
    }
}
