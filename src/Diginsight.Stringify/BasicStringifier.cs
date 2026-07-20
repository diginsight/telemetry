using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
#if NET || NETSTANDARD2_1_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace Diginsight.Stringify;

internal sealed class BasicStringifier : IStringifier
{
    private readonly IMemberInfoStringifier memberInfoStringifier;

    public BasicStringifier(
        IMemberInfoStringifier memberInfoStringifier
    )
    {
        this.memberInfoStringifier = memberInfoStringifier;
    }

    public IStringifiable? TryStringify(object obj)
    {
        switch (obj)
        {
#if NET || NETSTANDARD2_1_OR_GREATER
            case ITuple tuple:
                return new StringifiableTuple(tuple);
#endif

            case StringBuilder sb:
                return new StringifiableStringBuilder(sb);

            case Regex:
                return new DirectStringifiable(obj, "/{0}/");

            case Uri:
                return new DirectStringifiable(obj, StringifyTokens.LiteralBegin + "{0}" + StringifyTokens.LiteralEnd);

            case Index or Range:
                return new DirectStringifiable(obj);

            case DateTime or DateTimeOffset
#if NET
                or DateOnly or TimeOnly
#endif
                :
                return new DirectStringifiable(obj, StringifyTokens.LiteralBegin + "{0:O}" + StringifyTokens.LiteralEnd);

            case TimeSpan:
                return new DirectStringifiable(obj, StringifyTokens.LiteralBegin + "{0:g}" + StringifyTokens.LiteralEnd);

            case Guid:
                return new DirectStringifiable(obj, StringifyTokens.LiteralBegin + "{0:D}" + StringifyTokens.LiteralEnd);

            case Delegate del:
                return new StringifiableDelegate(del, this);

            case Expiration expiration:
                return new StringifiableExpiration(expiration);

            case JsonDocument jd:
                return new StringifiableJsonElement(jd.RootElement);

            case JsonElement je:
                return new StringifiableJsonElement(je);
        }

        Type type = obj.GetType();
        if (type.IsKeyValuePair(out Type? tKey, out Type? tValue))
        {
            return (IStringifiable)typeof(StringifiableKeyValuePair<,>)
                .MakeGenericType(tKey, tValue)
                .GetConstructors()[0]
                .Invoke([ obj ]);
        }

        return null;
    }

#if NET || NETSTANDARD2_1_OR_GREATER
    private sealed class StringifiableTuple : IStringifiable
    {
        private readonly ITuple tuple;

        object? IStringifiable.Subject => null;

        public StringifiableTuple(ITuple tuple)
        {
            this.tuple = tuple;
        }

        public void AppendTo(StringifyContext stringifyContext)
        {
            stringifyContext.AppendDelimited(
                StringifyTokens.TupleBegin,
                StringifyTokens.TupleEnd,
                sc =>
                {
                    AllottedCounter counter = AllottedCounter.Count(sc.VariableConfiguration.EffectiveMaxTupleItemCount);

                    try
                    {
                        void AppendItem(int i)
                        {
                            counter.Decrement();
                            sc.ThrowIfTimeIsOver();

                            sc.ComposeAndAppend(tuple[i]);
                        }

                        AppendItem(0);
                        for (int i = 1; i < tuple.Length; i++)
                        {
                            sc.AppendDirect(StringifyTokens.Separator2);
                            AppendItem(i);
                        }
                    }
                    catch (MaxAllottedShortCircuit)
                    {
                        sc.AppendEllipsis();
                    }
                }
            );
        }
    }
#endif

    private sealed class StringifiableStringBuilder : IStringifiable
    {
        private readonly StringBuilder stringBuilder;

        bool IStringifiable.IsDeep => false;
        object? IStringifiable.Subject => null;

        public StringifiableStringBuilder(StringBuilder stringBuilder)
        {
            this.stringBuilder = stringBuilder;
        }

        public void AppendTo(StringifyContext stringifyContext)
        {
            stringifyContext.AppendDirect(sb => sb.Append(stringBuilder));
        }
    }

    private sealed class StringifiableDelegate : IStringifiable
    {
        private readonly Delegate del;
        private readonly BasicStringifier owner;

#if !(NET || NETSTANDARD2_1_OR_GREATER)
        bool IStringifiable.IsDeep => true;
#endif
        object? IStringifiable.Subject => null;

        public StringifiableDelegate(Delegate del, BasicStringifier owner)
        {
            this.del = del;
            this.owner = owner;
        }

        public void AppendTo(StringifyContext stringifyContext)
        {
            stringifyContext.AppendDirect('λ');
            owner.memberInfoStringifier.Append(del.Method.GetParameters(), stringifyContext);
            stringifyContext.AppendDirect(':');
            owner.memberInfoStringifier.Append(del.Method.ReturnType, stringifyContext);
        }
    }

    private sealed class StringifiableExpiration : IStringifiable
    {
        private readonly Expiration expiration;

        bool IStringifiable.IsDeep => false;
        object? IStringifiable.Subject => null;

        public StringifiableExpiration(Expiration expiration)
        {
            this.expiration = expiration;
        }

        public void AppendTo(StringifyContext stringifyContext)
        {
            if (expiration.IsNever)
            {
                stringifyContext.AppendDirect($"{StringifyTokens.LiteralBegin}{Expiration.NeverString}{StringifyTokens.LiteralEnd}");
            }
            else
            {
                stringifyContext.ComposeAndAppend(expiration.Value);
            }
        }
    }

    private sealed class StringifiableKeyValuePair<TKey, TValue> : IStringifiable
    {
        private readonly KeyValuePair<TKey, TValue> kvp;

#if !(NET || NETSTANDARD2_1_OR_GREATER)
        bool IStringifiable.IsDeep => true;
#endif
        object? IStringifiable.Subject => null;

        public StringifiableKeyValuePair(KeyValuePair<TKey, TValue> kvp)
        {
            this.kvp = kvp;
        }

        public void AppendTo(StringifyContext stringifyContext)
        {
            stringifyContext
                .ComposeAndAppendType(kvp.GetType())
                .AppendDirect(StringifyTokens.MapBegin)
                .ComposeAndAppend(kvp.Key)
                .AppendDirect(StringifyTokens.Value)
                .ComposeAndAppend(kvp.Value)
                .AppendDirect(StringifyTokens.MapEnd);
        }
    }

    private sealed class StringifiableJsonElement : IStringifiable
    {
        private readonly JsonElement je;

#if !(NET || NETSTANDARD2_1_OR_GREATER)
        bool IStringifiable.IsDeep => true;
#endif
        object? IStringifiable.Subject => null;

        public StringifiableJsonElement(JsonElement je)
        {
            this.je = je;
        }

        public void AppendTo(StringifyContext stringifyContext)
        {
            switch (je.ValueKind)
            {
                case JsonValueKind.Object:
                {
                    stringifyContext.AppendDirect('{');
                    using (IEnumerator<JsonProperty> enumerator = je.EnumerateObject())
                    {
                        stringifyContext.AppendEnumerator(
                            enumerator,
                            static (sc, e) =>
                            {
                                JsonProperty jp = e.Current;
                                sc
                                    .AppendDirect(JsonSerializer.Serialize(jp.Name))
                                    .AppendDirect(':')
                                    .ComposeAndAppend(jp.Value);
                            },
                            stringifyContext.CountDictionaryItems(),
                            StringifyTokens.Separator1
                        );
                    }
                    stringifyContext.AppendDirect('}');

                    break;
                }

                case JsonValueKind.Array:
                {
                    stringifyContext.AppendDirect('[');
                    using (IEnumerator<JsonElement> enumerator = je.EnumerateArray())
                    {
                        stringifyContext.AppendEnumerator(
                            enumerator,
                            static (sc, e) => {sc.ComposeAndAppend(e.Current); },
                            stringifyContext.CountDictionaryItems(),
                            StringifyTokens.Separator1
                        );
                    }
                    stringifyContext.AppendDirect(']');

                    break;
                }

                case JsonValueKind.String:
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    stringifyContext.AppendDirect(JsonSerializer.Serialize(je));
                    break;

                case JsonValueKind.Undefined:
                    throw new InvalidOperationException($"Unsupported {nameof(JsonValueKind)}");

                default:
                    throw new UnreachableException($"Unrecognized {nameof(JsonValueKind)}");
            }
        }
    }
}
