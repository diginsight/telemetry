using System.Collections;
using System.Diagnostics;

namespace Diginsight.Stringify;

internal sealed class CollectionsStringifier : IStringifier
{
    public IStringifiable? TryStringify(object obj)
    {
        if (obj is IDictionary dict)
        {
            return new StringifiableDictionary(dict);
        }

        Type type = obj.GetType();
        if (type.IsIEnumerableOfKeyValuePair(out Type? tKey, out Type? tValue))
        {
            return (IStringifiable)typeof(StringifiableKvpCollection<,>)
                .MakeGenericType(tKey, tValue)
                .GetConstructors()[0]
                .Invoke([ obj ]);
        }

        if (type.IsIEnumerable(out Type? tInner0))
        {
            return (IStringifiable)typeof(StringifiableGenericCollection<>)
                .MakeGenericType(tInner0)
                .GetConstructors()[0]
                .Invoke([ obj ]);
        }

        if (type.IsIAsyncEnumerable(out Type? tInner1))
        {
            return (IStringifiable)typeof(StringifiableGenericAsyncCollection<>)
                .MakeGenericType(tInner1)
                .GetConstructors()[0]
                .Invoke([ obj ]);
        }

        if (obj is IEnumerable coll)
        {
            return new StringifiableCollection(coll);
        }

        return null;
    }

    private abstract class StringifiableCollectionBase<T> : IStringifiable
        where T : notnull
    {
        protected readonly T subject;

#if !(NET || NETSTANDARD2_1_OR_GREATER)
        bool IStringifiable.IsDeep => true;
#endif
        object IStringifiable.Subject => subject;

        protected abstract char BeginDelim { get; }
        protected abstract char EndDelim { get; }

        protected StringifiableCollectionBase(T subject)
        {
            this.subject = subject;
        }

        public void AppendTo(StringifyContext stringifyContext)
        {
            object? collectionLength = subject is Array ? GetLengths() : GetCount();
            stringifyContext.ComposeAndAppendType(subject.GetType(), collectionLength);

            stringifyContext.AppendDelimited(BeginDelim, EndDelim, AppendToCore);
        }

        protected abstract int? GetCount();

        protected abstract int[] GetLengths();

        protected abstract void AppendToCore(StringifyContext stringifyContext);
    }

    private sealed class StringifiableDictionary : StringifiableCollectionBase<IDictionary>
    {
        protected override char BeginDelim => StringifyTokens.MapBegin;
        protected override char EndDelim => StringifyTokens.MapEnd;

        public StringifiableDictionary(IDictionary subject)
            : base(subject) { }

        protected override int? GetCount() => subject.Count;

        protected override int[] GetLengths() => throw new UnreachableException("Unexpected array");

        protected override void AppendToCore(StringifyContext stringifyContext)
        {
            IDictionaryEnumerator enumerator = subject.GetEnumerator();

            try
            {
                stringifyContext
                    .AppendEnumerator(
                        enumerator,
                        static (sc, e) =>
                        {
                            sc
                                .ComposeAndAppend(e.Key)
                                .AppendDirect(StringifyTokens.Value)
                                .ComposeAndAppend(e.Value);
                        },
                        stringifyContext.CountDictionaryItems()
                    );
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }
    }

    private sealed class StringifiableKvpCollection<TKey, TValue> : StringifiableCollectionBase<IEnumerable<KeyValuePair<TKey, TValue>>>
    {
        protected override char BeginDelim => StringifyTokens.MapBegin;
        protected override char EndDelim => StringifyTokens.MapEnd;

        public StringifiableKvpCollection(IEnumerable<KeyValuePair<TKey, TValue>> subject)
            : base(subject) { }

        protected override int? GetCount()
        {
            return subject.TryGetNonEnumeratedCount(out int count) ? count : null;
        }

        protected override int[] GetLengths()
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            return [ ((Array)subject).Length ];
        }

        protected override void AppendToCore(StringifyContext stringifyContext)
        {
            using IEnumerator<KeyValuePair<TKey, TValue>> enumerator = subject.GetEnumerator();

            stringifyContext.AppendEnumerator(
                enumerator,
                static (sc, e) =>
                {
#if NET || NETSTANDARD2_1_OR_GREATER
                    (TKey key, TValue value) = e.Current;
#else
                    TKey key = e.Current.Key;
                    TValue value = e.Current.Value;
#endif
                    sc
                        .ComposeAndAppend(key)
                        .AppendDirect(StringifyTokens.Value)
                        .ComposeAndAppend(value);
                },
                stringifyContext.CountDictionaryItems()
            );
        }
    }

    private sealed class StringifiableGenericCollection<T> : StringifiableCollectionBase<IEnumerable<T>>
    {
        protected override char BeginDelim => StringifyTokens.CollectionBegin;
        protected override char EndDelim => StringifyTokens.CollectionEnd;

        public StringifiableGenericCollection(IEnumerable<T> subject)
            : base(subject) { }

        protected override int? GetCount()
        {
            return subject.TryGetNonEnumeratedCount(out int count) ? count : null;
        }

        protected override int[] GetLengths()
        {
            return [ ((T[])subject).Length ];
        }

        protected override void AppendToCore(StringifyContext stringifyContext)
        {
            using IEnumerator<T> enumerator = subject.GetEnumerator();

            stringifyContext.AppendEnumerator(
                enumerator,
                static (sc, e) => { sc.ComposeAndAppend(e.Current); },
                stringifyContext.CountCollectionItems()
            );
        }
    }

    private sealed class StringifiableGenericAsyncCollection<T> : IStringifiable
        where T : notnull
    {
        private readonly IAsyncEnumerable<T> subject;

#if !(NET || NETSTANDARD2_1_OR_GREATER)
        bool IStringifiable.IsDeep => true;
#endif
        object IStringifiable.Subject => subject;

        public StringifiableGenericAsyncCollection(IAsyncEnumerable<T> subject)
        {
            this.subject = subject;
        }

        public void AppendTo(StringifyContext stringifyContext)
        {
            stringifyContext.ComposeAndAppendType(subject.GetType());

            stringifyContext.AppendDelimited(StringifyTokens.CollectionBegin, StringifyTokens.CollectionEnd, AppendToCore);
        }

        private void AppendToCore(StringifyContext stringifyContext)
        {
            AppendToCoreAsync().GetAwaiter().GetResult();

            async Task AppendToCoreAsync()
            {
                await using IAsyncEnumerator<T> enumerator = subject.GetAsyncEnumerator();

                stringifyContext.AppendEnumerator(
                    SyncWrap(enumerator),
                    static (sc, e) => { sc.ComposeAndAppend(e.Current); },
                    stringifyContext.CountCollectionItems()
                );
            }
        }

        private static IEnumerator<T> SyncWrap(IAsyncEnumerator<T> enumerator)
        {
            while (enumerator.MoveNextAsync().GetAwaiter().GetResult())
            {
                yield return enumerator.Current;
            }
        }
    }

    private sealed class StringifiableCollection : StringifiableCollectionBase<IEnumerable>
    {
        protected override char BeginDelim => StringifyTokens.CollectionBegin;
        protected override char EndDelim => StringifyTokens.CollectionEnd;

        public StringifiableCollection(IEnumerable subject)
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

        protected override void AppendToCore(StringifyContext stringifyContext)
        {
            IEnumerator enumerator = subject.GetEnumerator();

            try
            {
                stringifyContext.AppendEnumerator(
                    enumerator,
                    static (sc, e) => { sc.ComposeAndAppend(e.Current); },
                    stringifyContext.CountCollectionItems()
                );
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }
    }
}
