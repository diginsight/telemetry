using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Diginsight.Strings;

internal sealed class CollectionsLogStringProvider : ILogStringProvider
{
    public ILogStringable? TryAsLogStringable(object obj)
    {
        if (obj is IDictionary dict)
        {
            return new LogStringableDictionary(dict);
        }

        Type type = obj.GetType();
        if (IsIEnumerableOfKeyValuePair(type, out Type? tKey, out Type? tValue))
        {
            return (ILogStringable)typeof(LogStringableKvpCollection<,>)
                .MakeGenericType(tKey, tValue)
                .GetConstructors()[0]
                .Invoke(new[] { obj });
        }

        if (IsIEnumerable(type, out Type? tInner))
        {
            return (ILogStringable)typeof(LogStringableGenericCollection<>)
                .MakeGenericType(tInner)
                .GetConstructors()[0]
                .Invoke(new[] { obj });
        }

        if (obj is IEnumerable coll)
        {
            return new LogStringableCollection(coll);
        }

        return null;
    }

    private static bool IsIEnumerable(Type type, [NotNullWhen(true)] out Type? tInner)
    {
        foreach (Type interfaceType in type.GetInterfaces())
        {
            if (!interfaceType.IsGenericType || interfaceType.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                continue;

            tInner = interfaceType.GetGenericArguments()[0];
            return true;
        }

        tInner = null;
        return false;
    }

    private static bool IsIEnumerableOfKeyValuePair(Type type, [NotNullWhen(true)] out Type? tKey, [NotNullWhen(true)] out Type? tValue)
    {
        foreach (Type interfaceType in type.GetInterfaces())
        {
            if (IsIEnumerable(interfaceType, out Type? innerType) && innerType.IsKeyValuePair(out tKey, out tValue))
            {
                return true;
            }
        }

        tKey = null;
        tValue = null;
        return false;
    }

    private abstract class LogStringableCollectionBase<T> : ILogStringable
        where T : notnull
    {
        protected readonly T subject;

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
        public bool IsDeep => true;
        public bool CanCycle => true;
#endif

        protected abstract char BeginToken { get; }
        protected abstract char EndToken { get; }

        protected LogStringableCollectionBase(T subject)
        {
            this.subject = subject;
        }

        public void AppendTo(StringBuilder stringBuilder, AppendingContext appendingContext)
        {
            try
            {
                object? collectionLength = subject is Array ? GetLengths() : GetCount();
                appendingContext.Append(
                    subject.GetType(),
                    stringBuilder,
                    false,
                    configureMetaProperties: x => { x[MemberLogStringProvider.CollectionLengthMetaProperty] = collectionLength; }
                );

                stringBuilder.Append(BeginToken);
                AppendToCore(stringBuilder, appendingContext);
                stringBuilder.Append(EndToken);
            }
            catch (AlreadySeenShortCircuit)
            {
                stringBuilder.Append(LogStringTokens.Cycle);
            }
        }

        protected abstract int? GetCount();

        protected abstract int[] GetLengths();

        protected abstract void AppendToCore(StringBuilder stringBuilder, AppendingContext appendingContext);
    }

    private sealed class LogStringableDictionary : LogStringableCollectionBase<IDictionary>
    {
        protected override char BeginToken => LogStringTokens.MapBegin;
        protected override char EndToken => LogStringTokens.MapEnd;

        public LogStringableDictionary(IDictionary subject)
            : base(subject) { }

        protected override int? GetCount() => subject.Count;

        protected override int[] GetLengths() => throw new UnreachableException("Unexpected array");

        protected override void AppendToCore(StringBuilder stringBuilder, AppendingContext appendingContext)
        {
            IDictionaryEnumerator enumerator = subject.GetEnumerator();
            if (!enumerator.MoveNext())
                return;

            AllottingCounter counter = appendingContext.CountDictionaryItems();
            try
            {
                void AppendEntry()
                {
                    counter.Decrement();
                    stringBuilder
                        .AppendLogString(enumerator.Key, appendingContext)
                        .Append(LogStringTokens.Value)
                        .AppendLogString(enumerator.Value, appendingContext);
                }

                AppendEntry();
                while (enumerator.MoveNext())
                {
                    stringBuilder.Append(LogStringTokens.Separator2);
                    AppendEntry();
                }
            }
            catch (MaxAllottedShortCircuit)
            {
                stringBuilder.Append(LogStringTokens.Ellipsis);
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }
    }

    private sealed class LogStringableKvpCollection<TKey, TValue> : LogStringableCollectionBase<IEnumerable<KeyValuePair<TKey, TValue>>>
    {
        protected override char BeginToken => LogStringTokens.MapBegin;
        protected override char EndToken => LogStringTokens.MapEnd;

        public LogStringableKvpCollection(IEnumerable<KeyValuePair<TKey, TValue>> subject)
            : base(subject) { }

        protected override int? GetCount()
        {
            return subject.TryGetNonEnumeratedCount(out int count) ? count : null;
        }

        protected override int[] GetLengths()
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            return new[] { ((Array)subject).Length };
        }

        protected override void AppendToCore(StringBuilder stringBuilder, AppendingContext appendingContext)
        {
            using IEnumerator<KeyValuePair<TKey, TValue>> enumerator = subject.GetEnumerator();
            if (!enumerator.MoveNext())
                return;

            AllottingCounter counter = appendingContext.CountDictionaryItems();
            try
            {
                void AppendEntry()
                {
                    counter.Decrement();
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    (TKey key, TValue value) = enumerator.Current;
#else
                    TKey key = enumerator.Current.Key;
                    TValue value = enumerator.Current.Value;
#endif
                    stringBuilder
                        .AppendLogString(key, appendingContext)
                        .Append(LogStringTokens.Value)
                        .AppendLogString(value, appendingContext);
                }

                AppendEntry();
                while (enumerator.MoveNext())
                {
                    stringBuilder.Append(LogStringTokens.Separator2);
                    AppendEntry();
                }
            }
            catch (MaxAllottedShortCircuit)
            {
                stringBuilder.Append(LogStringTokens.Ellipsis);
            }
        }
    }

    private sealed class LogStringableGenericCollection<T> : LogStringableCollectionBase<IEnumerable<T>>
    {
        protected override char BeginToken => LogStringTokens.CollectionBegin;
        protected override char EndToken => LogStringTokens.CollectionEnd;

        public LogStringableGenericCollection(IEnumerable<T> subject)
            : base(subject) { }

        protected override int? GetCount()
        {
            return subject.TryGetNonEnumeratedCount(out int count) ? count : null;
        }

        protected override int[] GetLengths()
        {
            return new[] { ((T[])subject).Length };
        }

        protected override void AppendToCore(StringBuilder stringBuilder, AppendingContext appendingContext)
        {
            using IEnumerator<T> enumerator = subject.GetEnumerator();
            if (!enumerator.MoveNext())
                return;

            AllottingCounter counter = appendingContext.CountCollectionItems();
            try
            {
                void AppendItem()
                {
                    counter.Decrement();
                    stringBuilder.AppendLogString(enumerator.Current, appendingContext);
                }

                AppendItem();
                while (enumerator.MoveNext())
                {
                    stringBuilder.Append(LogStringTokens.Separator2);
                    AppendItem();
                }
            }
            catch (MaxAllottedShortCircuit)
            {
                stringBuilder.Append(LogStringTokens.Ellipsis);
            }
        }
    }

    private sealed class LogStringableCollection : LogStringableCollectionBase<IEnumerable>
    {
        protected override char BeginToken => LogStringTokens.CollectionBegin;
        protected override char EndToken => LogStringTokens.CollectionEnd;

        public LogStringableCollection(IEnumerable subject)
            : base(subject) { }

        protected override int? GetCount()
        {
            return subject is ICollection coll ? coll.Count : null;
        }

        protected override int[] GetLengths()
        {
            Array array = (Array)subject;
            return Enumerable.Range(0, array.Rank).Select(array.GetLength).ToArray();
        }

        protected override void AppendToCore(StringBuilder stringBuilder, AppendingContext appendingContext)
        {
            IEnumerator enumerator = subject.GetEnumerator();
            if (!enumerator.MoveNext())
                return;

            AllottingCounter counter = appendingContext.CountCollectionItems();
            try
            {
                void AppendItem()
                {
                    counter.Decrement();
                    stringBuilder.AppendLogString(enumerator.Current, appendingContext);
                }

                AppendItem();
                while (enumerator.MoveNext())
                {
                    stringBuilder.Append(LogStringTokens.Separator2);
                    AppendItem();
                }
            }
            catch (MaxAllottedShortCircuit)
            {
                stringBuilder.Append(LogStringTokens.Ellipsis);
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }
    }
}
