using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Reflection;

namespace Diginsight.SmartCache;

public static class SmartCacheExtensions
{
    private static readonly MethodInfo UnwrapAsArrayMethod = typeof(SmartCacheExtensions)
        .GetMethod(nameof(UnwrapAsArray), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static IServiceCollection AddSmartCache(this IServiceCollection services, bool addMiddleware = true)
    {
        services.AddMemoryCache();

        services.TryAddSingleton<ISmartCacheService, SmartCacheService>();
        services.TryAddSingleton<ICacheKeyService, CacheKeyService>();

        services.TryAddSingleton<IRedisDatabaseAccessor, RedisDatabaseAccessor>();
        services.TryAddSingleton<RedisCacheLocation>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<SmartCacheRedisOptions>, ValidateSmartCacheRedisOptions>());

        if (addMiddleware && !services.Any(static x => x.ServiceType == typeof(SmartCacheMiddleware)))
        {
            services
                .AddTransient<SmartCacheMiddleware>()
                .AddSingleton<IValidateOptions<SmartCacheMiddlewareOptions>, ValidateSmartCacheMiddlewareOptions>();
        }

        return services;
    }

    private sealed class ValidateSmartCacheRedisOptions : IValidateOptions<SmartCacheRedisOptions>
    {
        public ValidateOptionsResult Validate(string? name, SmartCacheRedisOptions options)
        {
            if (name != Options.DefaultName)
            {
                return ValidateOptionsResult.Skip;
            }

            if (options.Configuration is not null && string.IsNullOrEmpty(options.KeyPrefix))
            {
                return ValidateOptionsResult.Fail($"{nameof(SmartCacheRedisOptions.KeyPrefix)} must be non-empty");
            }

            return ValidateOptionsResult.Success;
        }
    }

    private sealed class ValidateSmartCacheMiddlewareOptions : IValidateOptions<SmartCacheMiddlewareOptions>
    {
        public ValidateOptionsResult Validate(string? name, SmartCacheMiddlewareOptions options)
        {
            if (name != Options.DefaultName)
            {
                return ValidateOptionsResult.Skip;
            }

            ICollection<string> failureMessages = new List<string>();
            if (options.RootPath?[0] != '/')
            {
                failureMessages.Add($"{nameof(SmartCacheMiddlewareOptions.RootPath)} must be not null and start with '/'");
            }
            if (options.GetPathSegment is { } getPathSegment && getPathSegment[0] != '/')
            {
                failureMessages.Add($"{nameof(SmartCacheMiddlewareOptions.GetPathSegment)} must start with '/'");
            }
            if (options.CacheMissPathSegment is { } cacheMissPathSegment && cacheMissPathSegment[0] != '/')
            {
                failureMessages.Add($"{nameof(SmartCacheMiddlewareOptions.CacheMissPathSegment)} must start with '/'");
            }
            if (options.InvalidatePathSegment is { } invalidatePathSegment && invalidatePathSegment[0] != '/')
            {
                failureMessages.Add($"{nameof(SmartCacheMiddlewareOptions.InvalidatePathSegment)} must start with '/'");
            }

            return failureMessages.Count > 0 ? ValidateOptionsResult.Fail(failureMessages) : ValidateOptionsResult.Success;
        }
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
            .Invoke(null, [key])!;
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
