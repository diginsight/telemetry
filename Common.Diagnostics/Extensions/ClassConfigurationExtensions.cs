using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Primitives;
using System.ComponentModel;
using System.Globalization;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Common
{
    public static class ClassConfigurationExtensions
    {
        public static IServiceCollection AddClassConfiguration(this IServiceCollection services)
        {
            services.TryAddSingleton(typeof(IClassConfigurationGetter<>), typeof(ClassConfigurationGetter<>));
            services.TryAddSingleton<IClassConfigurationGetterProvider, ClassConfigurationGetterProvider>();

            services.TryAddScoped(typeof(IScopedClassConfigurationGetter<>), typeof(ScopedClassConfigurationGetter<>));
            services.AddScoped<IScopedConfiguration, ScopedConfiguration>();
            return services;
        }

        public static bool TryGetFromHeaders<T>(this IHeaderDictionary pthis, IEnumerable<string> prefixes, string key, out T value0)
        {
            foreach (string fullKey in prefixes.Select(x => x + key))
            {
                if (!pthis.TryGetValue(fullKey, out StringValues stringValues)) { continue; }

                try
                {
                    var converter = TypeDescriptor.GetConverter(typeof(T));
                    value0 = (T)converter.ConvertFrom(null, CultureInfo.InvariantCulture, stringValues.LastOrDefault());
                    return true;
                }
                catch (Exception e) { _ = e; }
            }

            value0 = default;
            return false;
        }
    }
}

