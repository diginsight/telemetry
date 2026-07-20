using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace Diginsight.Json;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class JsonNodeExtensions
{
    public static TResult Accept<TResult, TArg>(this JsonNode jnode, IJsonNodeVisitor<TResult, TArg> visitor, TArg arg)
#if NET9_0_OR_GREATER
        where TResult : allows ref struct
        where TArg : allows ref struct
#endif
    {
        return jnode switch
        {
            JsonArray x => visitor.Visit(x, arg),
            JsonObject x => visitor.Visit(x, arg),
            JsonValue x => visitor.Visit(x, arg),
            _ => throw new ArgumentOutOfRangeException(nameof(jnode)),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult Apply<TResult>(this IJsonNodeVisitor<TResult, ValueTuple> visitor, JsonNode jnode)
#if NET9_0_OR_GREATER
        where TResult : allows ref struct
#endif
    {
        return jnode.Accept(visitor, default);
    }

    extension(JsonNodeTransformer<ValueTuple> transformer)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JsonNode Apply(JsonNode jnode)
        {
            return transformer.Apply(jnode, out _);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JsonNode Apply(JsonNode jnode, out bool changed)
        {
            (JsonNode result, changed) = jnode.Accept(transformer, default);
            return result;
        }
    }
}
