using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Common
{
    public sealed class TraceSourceConfig
    {
        public string name { get; set; }
        public string switchName { get; set; }
        public string switchType { get; set; }
        public ListenerConfig[] listeners { get; set; }
    }
    public sealed class SwitchConfig
    {
        public string name { get; set; }
        public SourceLevels value { get; set; }
    }
    public sealed class ListenerFilterConfig
    {
        public string type { get; set; }
        public string initializeData { get; set; }
    }
    public sealed class ListenerConfig
    {
        public string action { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public ListenerConfig innerListener { get; set; }
        public ListenerFilterConfig filter { get; set; }
    }
    public sealed class SystemDiagnosticsConfig
    {
        public TraceSourceConfig[] sources { get; set; }
        public SwitchConfig[] switches { get; set; }
        public ListenerConfig[] sharedListeners { get; set; }
    }

    public class ScopedConfiguration : IScopedConfiguration
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IConfiguration configuration;
        private Dictionary<string, object> scopedValues = new Dictionary<string, object>();

        public ScopedConfiguration(IHttpContextAccessor httpContextAccessor,
                                    IConfiguration configuration)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.configuration = configuration;
        }

        public T? GetValue<T>(string key, T defaultValue)
        {
            if (scopedValues.TryGetValue(key, out var valueObject)) { return (T)valueObject; }

            if (httpContextAccessor.HttpContext?.Request.Headers[key].LastOrDefault() is { } rawValue)
            {
                object result;
                Exception error;
                TryConvertValue(typeof(T), rawValue, out result, out error);
                if (error != null) { throw error; }
                scopedValues[key] = result;
                return (T)result;
            }

            var value = configuration.GetValue<T>($"AppSettings:{key}", defaultValue);
            scopedValues[key] = value;
            return value;
        }

        #region ConvertValue
        private static object ConvertValue(Type type, string value, string path)
        {
            object result;
            Exception error;
            TryConvertValue(type, value, out result, out error);
            if (error != null) { throw error; }
            return result;
        }
        private static bool TryConvertValue(Type type, string value, out object result, out Exception error)
        {
            error = null;
            result = null;
            if (type == typeof(object))
            {
                result = value;
                return true;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (string.IsNullOrEmpty(value))
                {
                    return true;
                }
                return TryConvertValue(Nullable.GetUnderlyingType(type), value, out result, out error);
            }

            TypeConverter converter = TypeDescriptor.GetConverter(type);
            if (converter.CanConvertFrom(typeof(string)))
            {
                try
                {
                    result = converter.ConvertFromInvariantString(value);
                }
                catch (Exception ex)
                {
                    error = new InvalidOperationException("", ex); // SR.Format(SR.Error_FailedBinding, path, type)
                }
                return true;
            }

            if (type == typeof(byte[]))
            {
                try
                {
                    result = Convert.FromBase64String(value);
                }
                catch (FormatException ex)
                {
                    error = new InvalidOperationException("", ex); // SR.Format(SR.Error_FailedBinding, path, type)
                }
                return true;
            }

            return false;
        }
        #endregion
    }
    public class ScopedClassConfigurationGetter<TClass> : IScopedConfiguration
    {
        //private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IClassConfigurationGetter<TClass> configurationGetter;
        private Dictionary<string, object> scopedValues = new Dictionary<string, object>();

        public ScopedClassConfigurationGetter(IHttpContextAccessor httpContextAccessor,
                                              IClassConfigurationGetter<TClass> configurationGetter)
        {
            //this.httpContextAccessor = httpContextAccessor;
            this.configurationGetter = configurationGetter;
        }

        public T? GetValue<T>(string key, T defaultValue)
        {
            if (scopedValues.TryGetValue(key, out var valueObject)) { return (T)valueObject; }

            var value = configurationGetter.Get<T>($"{key}", defaultValue);
            scopedValues[key] = value;
            return value;
        }

        #region ConvertValue
        private static object ConvertValue(Type type, string value, string path)
        {
            object result;
            Exception error;
            TryConvertValue(type, value, out result, out error);
            if (error != null) { throw error; }
            return result;
        }
        private static bool TryConvertValue(Type type, string value, out object result, out Exception error)
        {
            error = null;
            result = null;
            if (type == typeof(object))
            {
                result = value;
                return true;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (string.IsNullOrEmpty(value))
                {
                    return true;
                }
                return TryConvertValue(Nullable.GetUnderlyingType(type), value, out result, out error);
            }

            TypeConverter converter = TypeDescriptor.GetConverter(type);
            if (converter.CanConvertFrom(typeof(string)))
            {
                try
                {
                    result = converter.ConvertFromInvariantString(value);
                }
                catch (Exception ex)
                {
                    error = new InvalidOperationException("", ex); // SR.Format(SR.Error_FailedBinding, path, type)
                }
                return true;
            }

            if (type == typeof(byte[]))
            {
                try
                {
                    result = Convert.FromBase64String(value);
                }
                catch (FormatException ex)
                {
                    error = new InvalidOperationException("", ex); // SR.Format(SR.Error_FailedBinding, path, type)
                }
                return true;
            }

            return false;
        }
        #endregion
    }
}
