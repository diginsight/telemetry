using Diginsight.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Diginsight.Stringify;

internal sealed class JsonNodeStringifier : IStringifier
{
    public IStringifiable? TryStringify(object obj)
    {
        return obj is JsonNode jn ? new StringifiableJsonNode(jn) : null;
    }

    private sealed class StringifiableJsonNode : IStringifiable, IJsonNodeVisitor<StringifyContext, StringifyContext>
    {
        private readonly JsonNode root;

        bool IStringifiable.IsDeep => root is JsonObject or JsonArray;
        object? IStringifiable.Subject => null;

        public StringifiableJsonNode(JsonNode root)
        {
            this.root = root;
        }

        public void AppendTo(StringifyContext stringifyContext)
        {
            stringifyContext.AppendDelimited(
                StringifyTokens.LiteralBegin,
                StringifyTokens.LiteralEnd,
                sc => { root.Accept(this, sc); }
            );
        }

        public StringifyContext Visit(JsonArray jarray, StringifyContext stringifyContext)
        {
            stringifyContext.AppendDirect('[');
            using (IEnumerator<JsonNode?> enumerator = jarray.GetEnumerator())
            {
                stringifyContext.AppendEnumerator(
                    enumerator,
                    (sc, e) =>
                    {
                        if (e.Current is null)
                            sc.AppendNull();
                        else
                            e.Current.Accept(this, sc);
                    },
                    stringifyContext.CountCollectionItems(),
                    StringifyTokens.Separator1
                );
            }
            stringifyContext.AppendDirect(']');

            return stringifyContext;
        }

        public StringifyContext Visit(JsonObject jobject, StringifyContext stringifyContext)
        {
            stringifyContext.AppendDirect('{');
            using (IEnumerator<KeyValuePair<string, JsonNode?>> enumerator = jobject.GetEnumerator())
            {
                stringifyContext.AppendEnumerator(
                    enumerator,
                    (sc, e) =>
                    {
                        (string pn, JsonNode? pv) = e.Current;
                        sc
                            .AppendDirect(JsonSerializer.Serialize(pn))
                            .AppendDirect(':');
                        if (pv is { } pv0)
                            pv0.Accept(this, sc);
                        else
                            sc.AppendNull();
                    },
                    stringifyContext.CountDictionaryItems(),
                    StringifyTokens.Separator1
                );
            }
            stringifyContext.AppendDirect('}');

            return stringifyContext;
        }

        public StringifyContext Visit(JsonValue jvalue, StringifyContext stringifyContext)
        {
            return stringifyContext.AppendDirect(JsonSerializer.Serialize(jvalue));
        }
    }
}
