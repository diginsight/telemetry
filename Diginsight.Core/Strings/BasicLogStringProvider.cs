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

    public ILogStringable? TryToLogStringable(object obj)
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

            case Index or Range:
                return new DirectLogStringable(obj);

            case DateTime or DateTimeOffset
#if NET6_0_OR_GREATER
                or DateOnly or TimeOnly
#endif
                :
                return new DirectLogStringable(obj, LogStringTokens.LiteralBegin + "{0:O}" + LogStringTokens.LiteralEnd);

            case TimeSpan:
                return new DirectLogStringable(obj, LogStringTokens.LiteralBegin + "{0:g}" + LogStringTokens.LiteralEnd);

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
                .Invoke([ obj ]);
        }

        return null;
    }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    private sealed class LogStringableTuple : ILogStringable
    {
        private readonly ITuple tuple;

        object? ILogStringable.Subject => null;

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
                            ac.ThrowIfTimeIsOver();

                            ac.ComposeAndAppend(tuple[i]);
                        }

                        AppendItem(0);
                        for (int i = 1; i < tuple.Length; i++)
                        {
                            ac.AppendDirect(LogStringTokens.Separator2);
                            AppendItem(i);
                        }
                    }
                    catch (MaxAllottedShortCircuit)
                    {
                        ac.AppendEllipsis();
                    }
                }
            );
        }
    }

    private sealed class LogStringableStringBuilder : ILogStringable
    {
        private readonly StringBuilder stringBuilder;

        bool ILogStringable.IsDeep => false;
        object? ILogStringable.Subject => null;

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
        bool ILogStringable.IsDeep => true;
#endif
        object? ILogStringable.Subject => null;

        public LogStringableDelegate(Delegate del, BasicLogStringProvider owner)
        {
            this.del = del;
            this.owner = owner;
        }

        public void AppendTo(AppendingContext appendingContext)
        {
            appendingContext.AppendDirect('λ');
            owner.memberInfoLogStringProvider.Append(del.Method.GetParameters(), appendingContext);
            appendingContext.AppendDirect(':');
            owner.memberInfoLogStringProvider.Append(del.Method.ReturnType, appendingContext);
        }
    }

    private sealed class LogStringableKeyValuePair<TKey, TValue> : ILogStringable
    {
        private readonly KeyValuePair<TKey, TValue> kvp;

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
        bool ILogStringable.IsDeep => true;
#endif
        object? ILogStringable.Subject => null;

        public LogStringableKeyValuePair(KeyValuePair<TKey, TValue> kvp)
        {
            this.kvp = kvp;
        }

        public void AppendTo(AppendingContext appendingContext)
        {
            appendingContext
                .ComposeAndAppendType(kvp.GetType())
                .AppendDirect(LogStringTokens.MapBegin)
                .ComposeAndAppend(kvp.Key)
                .AppendDirect(LogStringTokens.Value)
                .ComposeAndAppend(kvp.Value)
                .AppendDirect(LogStringTokens.MapEnd);
        }
    }
}
