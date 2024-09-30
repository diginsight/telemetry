using Diginsight.Stringify;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Diginsight.Json;

internal sealed class JTokenStringifier : IStringifier
{
    public IStringifiable? TryStringify(object obj)
    {
        return obj is JToken jt ? new StringifiableJToken(jt) : null;
    }

    private sealed class StringifiableJToken : IStringifiable, IJTokenVisitor<StringifyContext, StringifyContext>
    {
        private readonly JToken root;

        bool IStringifiable.IsDeep => root is JObject or JArray or JConstructor or JRaw;
        object? IStringifiable.Subject => null;

        public StringifiableJToken(JToken root)
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

        public StringifyContext Visit(JArray jarray, StringifyContext stringifyContext)
        {
            stringifyContext.AppendDirect('[');
            using (IEnumerator<JToken> enumerator = jarray.Children().GetEnumerator())
            {
                stringifyContext.AppendEnumerator(
                    enumerator,
                    (sc, e) => { e.Current.Accept(this, sc); },
                    stringifyContext.CountCollectionItems(),
                    ","
                );
            }
            stringifyContext.AppendDirect(']');

            return stringifyContext;
        }

        public StringifyContext Visit(JConstructor jconstructor, StringifyContext stringifyContext)
        {
            stringifyContext.AppendDirect($"new {jconstructor.Name}(");
            using (IEnumerator<JToken> enumerator = jconstructor.Children().GetEnumerator())
            {
                stringifyContext.AppendEnumerator(
                    enumerator,
                    (sc, e) => { e.Current.Accept(this, sc); },
                    stringifyContext.CountCollectionItems(),
                    ","
                );
            }
            stringifyContext.AppendDirect(')');

            return stringifyContext;
        }

        public StringifyContext Visit(JObject jobject, StringifyContext stringifyContext)
        {
            stringifyContext.AppendDirect('{');
            using (IEnumerator<JProperty> enumerator = jobject.Properties().GetEnumerator())
            {
                stringifyContext.AppendEnumerator(
                    enumerator,
                    (sc, e) => { e.Current.Accept(this, sc); },
                    stringifyContext.CountDictionaryItems(),
                    ","
                );
            }
            stringifyContext.AppendDirect('}');

            return stringifyContext;
        }

        public StringifyContext Visit(JProperty jproperty, StringifyContext stringifyContext)
        {
            stringifyContext
                .AppendDirect(new JValue(jproperty.Name).ToString(Formatting.None))
                .AppendDirect(':');

            return jproperty.Value.Accept(this, stringifyContext);
        }

        public StringifyContext Visit(JValue jvalue, StringifyContext stringifyContext)
        {
            return stringifyContext.AppendDirect(jvalue.ToString(Formatting.None));
        }
    }
}
