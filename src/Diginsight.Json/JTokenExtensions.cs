using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Diginsight.Json;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class JTokenExtensions
{
    public static TResult Accept<TResult, TArg>(this JToken jtoken, IJTokenVisitor<TResult, TArg> visitor, TArg arg)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult Apply<TResult>(this IJTokenVisitor<TResult, ValueTuple> visitor, JToken jtoken)
    {
        return jtoken.Accept(visitor, default);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JToken Apply(this JTokenTransformer<ValueTuple> transformer, JToken jtoken)
    {
        return transformer.Apply(jtoken, out _);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JToken Apply(this JTokenTransformer<ValueTuple> transformer, JToken jtoken, out bool changed)
    {
        (JToken result, changed) = jtoken.Accept(transformer, default);
        return result;
    }

    public static bool TryToObject<T>(this JToken jtoken, out T? obj, JsonSerializer? serializer = null)
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

    public static bool TryToObject(this JToken jtoken, Type type, out object? obj, JsonSerializer? serializer = null)
    {
        try
        {
            obj = jtoken.ToObject(type, serializer ?? JsonSerializer.CreateDefault());
            return true;
        }
        catch (Exception)
        {
            obj = default;
            return false;
        }
    }
}
