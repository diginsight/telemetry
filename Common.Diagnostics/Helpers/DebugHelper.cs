using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public class DebugHelper
    {
        #region internal state
        private static bool _isDebugBuild = false;
        //private static ConcurrentDictionary<string, object> _dicOverrides = new ConcurrentDictionary<string, object>();
        #endregion
        #region properties
        public static bool IsDebugBuild { get => _isDebugBuild; set => _isDebugBuild = value; }
        public static bool IsReleaseBuild { get => !_isDebugBuild; set => _isDebugBuild = !value; }
        public static ConcurrentDictionary<Type, bool> isDebugAssembly = new ConcurrentDictionary<Type, bool>();
        #endregion

        #region .ctor
        static DebugHelper()
        {
#if DEBUG
            IsDebugBuild = true;
#endif
        } 
        #endregion

        [Obsolete]
        public static void IfDebug(Action action)
        {
            if (!_isDebugBuild) { return; }
            action();
        }

        public static void IfDebug<T>(Action action)
        {
            var type = typeof(T);
            var assemply = type.Assembly;
            if (!isDebugAssembly.TryGetValue(type, out var isDebug))
            {
                isDebug = assemply.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled);
                isDebugAssembly.TryAdd(type, isDebug);
            }

            if (!isDebug) { return; }
            action();
        }

        public static bool IsTest(string environment = "Test")
        {
            var currentEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return environment.Equals(currentEnvironment, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsEnvironment(string environment)
        {
            var currentEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return environment.Equals(currentEnvironment, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsDevOrTest(string[] environments = null)
        {
            environments ??= new[] { "Development", "Test" };
            var currentEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            return environments.Any(environment => environment.Equals(currentEnvironment, StringComparison.InvariantCultureIgnoreCase));
        }

        public static void IfDebug(Action action, bool rethrowExceptions = false)
        {
            try
            {
                if (!IsDebugBuild) { return; }

                action();
            }
            catch (Exception ex)
            {
                TraceLogger.LogException(ex);
                if (rethrowExceptions)
                {
                    throw;
                }
            }
        }

        public static void IfDevOrTest(Action action, Action fallbackAction = null)
        {
            if (IsDevOrTest())
            {
                action();
            }
            else
            {
                fallbackAction?.Invoke();
            }
        }

        public static string GetEnvironment()
        {
            return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        }
    }

    internal class LogLevelHelper
    {
        public static TraceEventType ToTraceEventType(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace: return TraceEventType.Verbose;
                case LogLevel.Debug: return TraceEventType.Verbose;
                case LogLevel.Information: return TraceEventType.Information;
                case LogLevel.Warning: return TraceEventType.Warning;
                case LogLevel.Error: return TraceEventType.Error;
                case LogLevel.Critical: return TraceEventType.Critical;
                case LogLevel.None: return TraceEventType.Verbose;
                default: break;
            }
            return TraceEventType.Verbose;
        }
        public static SourceLevels ToSourceLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace: return SourceLevels.Verbose;
                case LogLevel.Debug: return SourceLevels.Verbose;
                case LogLevel.Information: return SourceLevels.Information;
                case LogLevel.Warning: return SourceLevels.Warning;
                case LogLevel.Error: return SourceLevels.Error;
                case LogLevel.Critical: return SourceLevels.Critical;
                case LogLevel.None: return SourceLevels.Verbose;
                default: break;
            }
            return SourceLevels.Verbose;
        }
    }

    internal static class ThreadHelper
    {
        public static void WaitUntil(Func<bool> condition, int ms = 20)
        {
            while (!condition()) { Thread.Sleep(ms); }
        }
    }
}
