using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace Diginsight.Json;

/// <summary>
/// Provides extension methods for JSON nodes.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class JsonNodeExtensions
{
    /// <summary>
    /// Dispatches the specified <see cref="JsonNode" /> to the matching <see cref="IJsonNodeVisitor{TResult, TArg}" /> method according to its runtime type.
    /// </summary>
    /// <typeparam name="TResult">The type of the result produced by the visitor.</typeparam>
    /// <typeparam name="TArg">The type of the argument passed to the visitor.</typeparam>
    /// <param name="jnode">The JSON node to visit.</param>
    /// <param name="visitor">The visitor that processes the node.</param>
    /// <param name="arg">The argument passed to the visitor.</param>
    /// <returns>The result produced by the visitor.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the runtime type of <paramref name="jnode" /> is not supported.</exception>
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

    /// <summary>
    /// Applies the specified visitor to the given <see cref="JsonNode" /> using the default argument.
    /// </summary>
    /// <typeparam name="TResult">The type of the result produced by the visitor.</typeparam>
    /// <param name="visitor">The visitor that processes the node.</param>
    /// <param name="jnode">The JSON node to visit.</param>
    /// <returns>The result produced by the visitor.</returns>
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
        /// <summary>
        /// Applies the transformer to the specified <see cref="JsonNode" /> and returns the transformed node.
        /// </summary>
        /// <param name="jnode">The JSON node to transform.</param>
        /// <returns>The transformed JSON node.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JsonNode Apply(JsonNode jnode)
        {
            return transformer.Apply(jnode, out _);
        }

        /// <summary>
        /// Applies the transformer to the specified <see cref="JsonNode" /> and reports whether the node was changed.
        /// </summary>
        /// <param name="jnode">The JSON node to transform.</param>
        /// <param name="changed">When this method returns, contains <c>true</c> if the node was changed; otherwise, <c>false</c>.</param>
        /// <returns>The transformed JSON node.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JsonNode Apply(JsonNode jnode, out bool changed)
        {
            (JsonNode result, changed) = jnode.Accept(transformer, default);
            return result;
        }
    }
}
