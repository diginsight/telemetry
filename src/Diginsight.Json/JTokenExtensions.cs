using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Diginsight.Json;

/// <summary>
/// Provides extension methods for JSON tokens.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class JTokenExtensions
{
    /// <param name="jtoken">The JSON token.</param>
    extension(JToken jtoken)
    {
        /// <summary>
        /// Accepts a JSON token visitor.
        /// </summary>
        /// <typeparam name="TResult">The type of the visitor result.</typeparam>
        /// <typeparam name="TArg">The type of the visitor argument.</typeparam>
        /// <param name="visitor">The JSON token visitor.</param>
        /// <param name="arg">The visitor argument.</param>
        /// <returns>The visitor result.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the JSON token type is not supported.</exception>
        public TResult Accept<TResult, TArg>(IJTokenVisitor<TResult, TArg> visitor, TArg arg)
#if NET9_0_OR_GREATER
            where TResult : allows ref struct
            where TArg : allows ref struct
#endif
        {
            return jtoken switch
            {
                JArray x => visitor.Visit(x, arg),
                JConstructor x => visitor.Visit(x, arg),
                JObject x => visitor.Visit(x, arg),
                JProperty x => visitor.Visit(x, arg),
                JRaw x => visitor.Visit(x, arg),
                JValue x => visitor.Visit(x, arg),
                _ => throw new ArgumentOutOfRangeException(nameof(jtoken)),
            };
        }

        /// <summary>
        /// Tries to convert the JSON token to an object of the specified type.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="obj">When this method returns <c>true</c>, contains the converted object; otherwise, the default value.</param>
        /// <param name="serializer">The JSON serializer to use.</param>
        /// <returns><c>true</c> if the JSON token was successfully converted; otherwise, <c>false</c>.</returns>
        public bool TryToObject<T>(out T? obj, JsonSerializer? serializer = null)
        {
            try
            {
                obj = jtoken.ToObject<T>(serializer ?? JsonSerializer.CreateDefault());
                return true;
            }
            catch (Exception)
            {
                obj = default;
                return false;
            }
        }

        /// <summary>
        /// Tries to convert the JSON token to an object of the specified type.
        /// </summary>
        /// <param name="type">The object type.</param>
        /// <param name="obj">When this method returns <c>true</c>, contains the converted object; otherwise, <c>null</c>.</param>
        /// <param name="serializer">The JSON serializer to use.</param>
        /// <returns><c>true</c> if the JSON token was successfully converted; otherwise, <c>false</c>.</returns>
        public bool TryToObject(Type type, out object? obj, JsonSerializer? serializer = null)
        {
            try
            {
                obj = jtoken.ToObject(type, serializer ?? JsonSerializer.CreateDefault());
                return true;
            }
            catch (Exception)
            {
                obj = null;
                return false;
            }
        }
    }

    /// <summary>
    /// Applies a JSON token visitor without an argument.
    /// </summary>
    /// <typeparam name="TResult">The type of the visitor result.</typeparam>
    /// <param name="visitor">The JSON token visitor.</param>
    /// <param name="jtoken">The JSON token.</param>
    /// <returns>The visitor result.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult Apply<TResult>(this IJTokenVisitor<TResult, ValueTuple> visitor, JToken jtoken)
#if NET9_0_OR_GREATER
        where TResult : allows ref struct
#endif
    {
        return jtoken.Accept(visitor, default);
    }

    /// <param name="transformer">The JSON token transformer.</param>
    extension(JTokenTransformer<ValueTuple> transformer)
    {
        /// <summary>
        /// Applies the JSON token transformer.
        /// </summary>
        /// <param name="jtoken">The JSON token.</param>
        /// <returns>The transformed JSON token.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JToken Apply(JToken jtoken)
        {
            return transformer.Apply(jtoken, out _);
        }

        /// <summary>
        /// Applies the JSON token transformer.
        /// </summary>
        /// <param name="jtoken">The JSON token.</param>
        /// <param name="changed">When this method returns, contains a value indicating whether the JSON token was changed.</param>
        /// <returns>The transformed JSON token.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JToken Apply(JToken jtoken, out bool changed)
        {
            (JToken result, changed) = jtoken.Accept(transformer, default);
            return result;
        }
    }
}
