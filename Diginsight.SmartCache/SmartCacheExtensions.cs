using Diginsight.SmartCache.Externalization;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Reflection;

namespace Diginsight.SmartCache;

public static partial class SmartCacheExtensions
{
    private static readonly MethodInfo UnwrapAsArrayMethod = typeof(SmartCacheExtensions)
        .GetMethod(nameof(UnwrapAsArray), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static SmartCacheBuilder AddSmartCache(this IServiceCollection services, Action<SmartCacheCoreOptions>? configureOptions = null)
    {
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        return new SmartCacheBuilder(services)
            .SetSizeLimit(10_000_000)
            .SetLocalCompanion();
    }

    public static ICacheKey ToKey(this ICacheKeyService cacheKeyService, object obj)
    {
        return cacheKeyService.TryToKey(obj, out ICacheKey? key)
            ? key
            : throw new NotSupportedException($"object not convertible to {nameof(ICacheKey)}");
    }

    public static ICacheKey Wrap<TSource>(
        this ICacheKeyService cacheKeyService, IEnumerable<TSource>? source
    )
    {
        return new EquatableArray(
            (source ?? Enumerable.Empty<TSource>())
            .Select(x => cacheKeyService.TryToKey(x, out ICacheKey? key) ? key : (object?)x)
            .ToArray()
        );
    }

    public static ICacheKey Wrap<TSource, TOrder>(
        this ICacheKeyService cacheKeyService, IEnumerable<TSource>? source, Func<TSource, TOrder> order, IComparer<TOrder>? comparer = null
    )
    {
        return new EquatableArray(
            (source ?? Enumerable.Empty<TSource>())
            .OrderBy(order, comparer)
            .Select(x => cacheKeyService.TryToKey(x, out ICacheKey? key) ? key : (object?)x)
            .ToArray()
        );
    }

    public static T UnwrapAs<T>(this ICacheKey key)
    {
        Type type = typeof(T);
        if (!type.IsArray ||
            type.GetElementType()! is var elementType && elementType == typeof(object))
        {
            return key.UnwrapAsPlain<T>();
        }

        return (T)UnwrapAsArrayMethod
            .MakeGenericMethod(elementType)
            .Invoke(null, [ key ])!;
    }

    private static T UnwrapAsPlain<T>(this ICacheKey key)
    {
        return (T)((IUnwrappable)key).Unwrap();
    }

    private static T[] UnwrapAsArray<T>(this ICacheKey key)
    {
        return Array.ConvertAll(key.UnwrapAsPlain<object[]>(), static x => (T)x);
    }

    [CacheInterchangeName("EA")]
    private sealed class EquatableArray
        : IEquatable<EquatableArray>, ICacheKey, IUnwrappable
    {
        [JsonProperty(ItemConverterType = typeof(Converter))]
        private readonly object?[] array;

        public EquatableArray(object?[] array)
        {
            this.array = array;
        }

        public bool Equals(EquatableArray? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return ((IStructuralEquatable)array).Equals(other.array, EqualityComparer<object>.Default);
        }

        public override bool Equals(object? obj) => Equals(obj as EquatableArray);

        public override int GetHashCode()
        {
            return ((IStructuralEquatable)array).GetHashCode(EqualityComparer<object>.Default);
        }

        public override string ToString()
        {
            return "[" + string.Join(",", array) + "]";
        }

        public object Unwrap()
        {
            return Array.ConvertAll(array, static x => x is IUnwrappable u ? u.Unwrap() : x);
        }

        private sealed class Converter : JsonConverter
        {
            public override bool CanRead => false;

            public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
            {
                if (value is null)
                {
                    writer.WriteNull();
                    return;
                }

                JToken tempJt;
                using (JTokenWriter tempWriter = new ())
                {
                    serializer.Serialize(tempWriter, value, typeof(object));
                    tempJt = tempWriter.Token!;
                }

                Type objectType = value.GetType();
                if ((tempJt is JObject && tempJt["$type"] is not null)
                    ||
                    (tempJt is JValue && JsonConvert.DeserializeObject<JValue>(tempJt.ToString(Formatting.None))!.Value!.GetType() == objectType))
                {
                    tempJt.WriteTo(writer);
                    return;
                }

                writer.WriteStartObject();

                writer.WritePropertyName("$type", false);
                serializer.Serialize(writer, objectType);
                writer.WritePropertyName("$value", false);
                tempJt.WriteTo(writer);

                writer.WriteEndObject();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
            {
                throw new NotSupportedException();
            }

            public override bool CanConvert(Type objectType)
            {
                throw new NotSupportedException();
            }
        }
    }
}
