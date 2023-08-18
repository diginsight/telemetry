using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Common
{
    internal sealed class ClassConfigurationGetter<TClass> : IClassConfigurationGetter<TClass>
    {
        private static readonly string[] Prefixes;

        private readonly IHttpContextAccessor httpContextAccessor;

        private readonly IConfiguration configuration;
        private readonly Dictionary<string, object> cache = new Dictionary<string, object>();

        static ClassConfigurationGetter()
        {
            Prefixes = GetPrefixes().ToArray();
        }
        static IEnumerable<string> GetPrefixes()
        {
            Type type = typeof(TClass);

            string[] namespacePieces = (type.Namespace ?? "").Split('.');
            IEnumerable<string> namespaceSegments = Enumerable.Range(1, namespacePieces.Length)
                    .Select(i => string.Join(".", namespacePieces.Take(i))).Reverse().ToArray();

            var availableShorthands = type.Assembly.GetCustomAttributes<ClassConfigurationNamespaceShorthandAttribute>()
                .ToDictionary(x => x.Namespace, x => x.Shorthand);
            IEnumerable<string> namespaceShorthands = namespaceSegments
                .Select(x => availableShorthands.TryGetValue(x, out var val) ? val : null).OfType<string>().ToArray();

            if (type.FullName != null)
            {
                yield return $"{type.FullName}.";
            }

            foreach (string shorthand in namespaceShorthands)
            {
                yield return $"#{shorthand}.{type.Name}.";
            }

            yield return $"{type.Name}.";

            foreach (string segment in namespaceSegments)
            {
                yield return $"{segment}.*.";
            }

            foreach (string shorthand in namespaceShorthands)
            {
                yield return $"#{shorthand}.*.";
            }

            yield return "";
        }

        public ClassConfigurationGetter(
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor
            )
        {
            this.configuration = configuration.GetSection("AppSettings");
            this.httpContextAccessor = httpContextAccessor;
        }

        public IClassConfigurationGetter<TClass> Empty => throw new NotImplementedException();

        IClassConfigurationGetter IClassConfigurationGetter.Empty => throw new NotImplementedException();



        public T Get<T>(string key, T defaultValue)
        {
            var requestHeaders = httpContextAccessor.HttpContext?.Request?.Headers;
            if (requestHeaders != null)
            {
                if (requestHeaders.TryGetFromHeaders(Prefixes, key, out T value1))
                {
                    return value1;
                }
            }

            if (cache.TryGetValue(key, out object rawValue))
            {
                return (T)rawValue;
            }

            lock (((ICollection)cache).SyncRoot)
            {
                if (cache.TryGetValue(key, out rawValue))
                {
                    return (T)rawValue;
                }

                T CoreGet()
                {
                    foreach (string fullKey in Prefixes.Select(x => x + key))
                    {
                        if (configuration.GetSection(fullKey).Value is null)
                        {
                            continue;
                        }

                        try
                        {
                            return configuration.GetValue<T>(fullKey);
                        }
                        catch (Exception e)
                        {
                            _ = e;
                        }
                    }

                    return defaultValue;
                }

                T finalValue = CoreGet();
                cache[key] = finalValue;
                return finalValue;
            }
        }
    }
}

