using System.Text;
using System.Text.RegularExpressions;
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace Diginsight.Strings;

internal sealed class BasicLogStringProvider : ILogStringProvider
{
    private readonly IMemberInfoLogStringProvider memberInfoLogStringProvider;

    public BasicLogStringProvider(
        IMemberInfoLogStringProvider memberInfoLogStringProvider
    )
    {
        this.memberInfoLogStringProvider = memberInfoLogStringProvider;
    }

    public ILogStringable? TryAsLogStringable(object obj)
    {
        switch (obj)
        {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            case ITuple tuple:
                return new LogStringableTuple(tuple);
#endif

            case StringBuilder sb:
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                return new LogStringableStringBuilder(sb);
#else
                return new DirectLogStringable(sb);
#endif

            case Regex:
                return new DirectLogStringable(obj, "/{0}/");

            case Uri:
                return new DirectLogStringable(obj, LogStringTokens.LiteralBegin + "{0}" + LogStringTokens.LiteralEnd);

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            case Index or Range:
                return new DirectLogStringable(obj);
#endif

            case DateTime or DateTimeOffset or TimeSpan
#if NET6_0_OR_GREATER
                or DateOnly or TimeOnly
#endif
                :
                return new DirectLogStringable(obj, LogStringTokens.LiteralBegin + "{0:O}" + LogStringTokens.LiteralEnd);

            case Guid:
                return new DirectLogStringable(obj, LogStringTokens.LiteralBegin + "{0:D}" + LogStringTokens.LiteralEnd);

            case Delegate del:
                return new LogStringableDelegate(del, this);
        }

        Type type = obj.GetType();
        if (type.IsKeyValuePair(out Type? tKey, out Type? tValue))
        {
            return (ILogStringable)typeof(LogStringableKeyValuePair<,>)
                .MakeGenericType(tKey, tValue)
                .GetConstructors()[0]
                .Invoke(new[] { obj });
        }

        return null;
    }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    private sealed class LogStringableTuple : ILogStringable
    {
        private readonly ITuple tuple;

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
        public bool IsDeep => true;
#endif
        public bool CanCycle => false;

        public LogStringableTuple(ITuple tuple)
        {
            this.tuple = tuple;
        }

        public void AppendTo(AppendingContext appendingContext)
        {
            appendingContext.AppendDelimited(
                LogStringTokens.TupleBegin,
                LogStringTokens.TupleEnd,
                ac =>
                {
                    AllottingCounter counter = AllottingCounter.Count(ac.VariableConfiguration.GetEffectiveMaxTupleItemCount());

                    try
                    {
                        void AppendItem(int i)
                        {
                            counter.Decrement();
                            ac.ComposeAndAppend(tuple[i]);
                        }

                        AppendItem(0);
                        for (int i = 1; i < tuple.Length; i++)
                        {
                            ac.AppendPunctuation(LogStringTokens.Separator2);
                            AppendItem(i);
                        }
                    }
                    catch (MaxAllottedCountShortCircuit)
                    {
                        ac.AppendPunctuation(LogStringTokens.Ellipsis);
                    }
                }
            );
        }
    }

    private sealed class LogStringableStringBuilder : ILogStringable
    {
        private readonly StringBuilder stringBuilder;

        public bool IsDeep => false;
        public bool CanCycle => false;

        public LogStringableStringBuilder(StringBuilder stringBuilder)
        {
            this.stringBuilder = stringBuilder;
        }

        public void AppendTo(AppendingContext appendingContext)
        {
            appendingContext.AppendDirect(sb => sb.Append(stringBuilder));
        }
    }
#endif

    private sealed class LogStringableDelegate : ILogStringable
    {
        private readonly Delegate del;
        private readonly BasicLogStringProvider owner;

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
        public bool IsDeep => true;
#endif
        public bool CanCycle => false;

        public LogStringableDelegate(Delegate del, BasicLogStringProvider owner)
        {
            this.del = del;
            this.owner = owner;
        }

        public void AppendTo(AppendingContext appendingContext)
        {
            appendingContext.AppendPunctuation('λ');
            owner.memberInfoLogStringProvider.Append(del.Method.GetParameters(), appendingContext);
            appendingContext.AppendPunctuation(':');
            owner.memberInfoLogStringProvider.Append(del.Method.ReturnType, appendingContext);
        }
    }

    private sealed class LogStringableKeyValuePair<TKey, TValue> : ILogStringable
    {
        private readonly KeyValuePair<TKey, TValue> kvp;

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
        public bool IsDeep => true;
#endif
        public bool CanCycle => false;

        public LogStringableKeyValuePair(KeyValuePair<TKey, TValue> kvp)
        {
            this.kvp = kvp;
        }

        public void AppendTo(AppendingContext appendingContext)
        {
            appendingContext
                .ComposeAndAppend(kvp.GetType(), false)
                .AppendPunctuation(LogStringTokens.MapBegin)
                .ComposeAndAppend(kvp.Key)
                .AppendPunctuation(LogStringTokens.Value)
                .ComposeAndAppend(kvp.Value)
                .AppendPunctuation(LogStringTokens.MapEnd);
        }
    }
}
