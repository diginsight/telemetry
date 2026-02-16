using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Diginsight.Json;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class JTokenExtensions
{
    extension(JToken jtoken)
    {
        public TResult Accept<TResult, TArg>(IJTokenVisitor<TResult, TArg> visitor, TArg arg)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult Apply<TResult>(this IJTokenVisitor<TResult, ValueTuple> visitor, JToken jtoken)
    {
        return jtoken.Accept(visitor, default);
    }

    extension(JTokenTransformer<ValueTuple> transformer)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JToken Apply(JToken jtoken)
        {
            return transformer.Apply(jtoken, out _);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JToken Apply(JToken jtoken, out bool changed)
        {
            (JToken result, changed) = jtoken.Accept(transformer, default);
            return result;
        }
    }
}
