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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Common
{
    public sealed class ClassConfigurationGetter<TClass> : IClassConfigurationGetter<TClass>
    {
        private static readonly string[] Prefixes;
        private ILogger<ClassConfigurationGetter<TClass>> logger;

        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IConfiguration configuration;
        private readonly Dictionary<string, object> cache = new Dictionary<string, object>();
        public IClassConfigurationGetter<TClass> Empty => EmptyClassConfigurationGetter<TClass>._empty;
        IClassConfigurationGetter IClassConfigurationGetter.Empty => EmptyClassConfigurationGetter<TClass>._empty;

        static ClassConfigurationGetter()
        {
            Prefixes = GetPrefixes().ToArray();
        }

        public ClassConfigurationGetter(
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor = null)
        {
            this.configuration = configuration.GetSection("AppSettings");
            this.httpContextAccessor = httpContextAccessor;
        }

        public T Get<T>(string key, T defaultValue)
        {
            var requestHeaders = httpContextAccessor?.HttpContext?.Request?.Headers;
            if (requestHeaders?.TryGetFromHeaders(Prefixes, key, out T value1) ?? false)
            {
                return value1;
            }

            T CoreGet()
            {
                var ticksStart = TraceLogger.Stopwatch.ElapsedTicks;
                //var ticksStart = TraceLogger.Stopwatch.ElapsedMilliseconds;
                //using (var scope = logger.BeginMethodScope(new Func<object>(() => new { key }), System.Diagnostics.SourceLevels.Verbose, LogLevel.Trace))
                //{
                foreach (string fullKey in Prefixes.Select(x => x + key))
                {
                    if (configuration.GetSection(fullKey).Value is null) { continue; }

                    try
                    {
                        var res = configuration.GetValue<T>(fullKey);
                        var ticksEnd = TraceLogger.Stopwatch.ElapsedTicks;
                        //var ticksEnd = TraceLogger.Stopwatch.ElapsedMilliseconds;
                        //TraceLogger.LogTrace($"configuration.GetValue<T>({fullKey}) retuned {res} (ticks: {ticksEnd - ticksStart:#,##0})");
                        return res;
                    }
                    catch (Exception e)
                    {
                        var ticksEnd = TraceLogger.Stopwatch.ElapsedTicks;
                        //TraceLogger.LogError($"configuration.GetValue<T>({fullKey}) failed with {e.Message} (ticks: {ticksEnd - ticksStart:#,##0})");
                        //TraceLogger.LogException(e);
                    }
                }
                //}

                return defaultValue;
            }

            object rawValue = default(T);
            lock (((ICollection)cache).SyncRoot) { if (cache.TryGetValue(key, out rawValue)) { return (T)rawValue; } }

            T finalValue = CoreGet();

            lock (((ICollection)cache).SyncRoot) { cache[key] = finalValue; }

            return finalValue;
        }

        static IEnumerable<string> GetPrefixes()
        {
            Type type = typeof(TClass);

            string[] namespacePieces = (type.Namespace ?? "").Split('.');
            IEnumerable<string> namespaceSegments = Enumerable.Range(1, namespacePieces.Length).Select(i => string.Join(".", namespacePieces.Take(i))).Reverse().ToArray();

            var availableShorthands = type.Assembly.GetCustomAttributes<ClassConfigurationNamespaceShorthandAttribute>().ToDictionary(x => x.Namespace, x => x.Shorthand);

            IEnumerable<string> namespaceShorthands = namespaceSegments.Select(x => availableShorthands.TryGetValue(x, out var val) ? val : null).OfType<string>().ToArray();

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
    }
}

