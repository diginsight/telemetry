#region using
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
#endregion

namespace Common
{
    public static class TraceLoggerExtensions
    {
        public static CodeSectionScope BeginMethodScope<T>(this ILogger<T> logger, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel logLevel = LogLevel.Debug, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var host = TraceLogger.Host;
            if (logger == null && host != null)
            {
                logger = host.Services?.GetService<ILogger<T>>();
            }
            //catch (Exception) { }
            //var loggerFactory = TraceLogger.LoggerFactory;
            //logger = loggerFactory.CreateLogger<T>();
            //if (logger == null) { return null; }
            if (host != null)
            {
                var traceLoggerMinimumLevel = host.Services?.GetService<ITraceLoggerMinimumLevel>();
                var minimumLevel = traceLoggerMinimumLevel?.MinimumLevel;
                if (minimumLevel.HasValue && minimumLevel.Value > logLevel) { return null; }
            }

            var sec = new CodeSectionScope(logger, typeof(T), null, payload, TraceLogger.TraceSource, sourceLevel, logLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);
            var stopTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var delta = stopTicks - startTicks;
            return sec;
        }
        public static CodeSectionScope BeginMethodScope<T>(this IHost host, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel logLevel = LogLevel.Debug, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            ILogger<T> logger = null;
            if (host == null) { host = TraceLogger.Host; }
            if (host != null) { logger = host.Services.GetRequiredService<ILogger<T>>(); }

            //var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            //if (loggerFactory == null) { loggerFactory = TraceLogger.LoggerFactory; }
            //logger = loggerFactory.CreateLogger<T>();

            var sec = new CodeSectionScope(logger, typeof(T), null, payload, TraceLogger.TraceSource, sourceLevel, logLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);
            var stopTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var delta = stopTicks - startTicks;
            return sec;
        }
        public static CodeSectionScope BeginMethodScope(this IHost host, Type t, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel logLevel = LogLevel.Debug, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            ILogger logger = null;
            if (host == null) { host = TraceLogger.Host; }

            //var loggerFactory = host?.Services?.GetRequiredService<ILoggerFactory>();
            //if (loggerFactory == null) { loggerFactory = TraceLogger.LoggerFactory; }

            if (host != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { t });
                logger = host.Services.GetRequiredService(loggerType) as ILogger;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var sec = new CodeSectionScope(logger, t, null, payload, TraceLogger.TraceSource, sourceLevel, logLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);
            var stopTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var delta = stopTicks - startTicks;
            return sec;
        }

        public static CodeSectionScope BeginNamedScope<T>(this ILogger<T> logger, string name, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel logLevel = LogLevel.Debug, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            if (logger == null && TraceLogger.Host != null)
            {
                var host = TraceLogger.Host;
                try { logger = host.Services?.GetRequiredService<ILogger<T>>(); }
                catch (Exception) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger<T>();
            }
            //if (logger == null) { return null; }

            var sec = new CodeSectionScope(logger, typeof(T), name, payload, TraceLogger.TraceSource, sourceLevel, logLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);
            var stopTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var delta = stopTicks - startTicks;
            return sec;
        }
        public static CodeSectionScope BeginMethodScope<T>(this IHost host, string name, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel logLevel = LogLevel.Debug, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            ILogger<T> logger = null;
            if (host == null) { host = TraceLogger.Host; }
            if (host != null) { logger = host.Services.GetRequiredService<ILogger<T>>(); }

            //var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            //if (loggerFactory == null) { loggerFactory = TraceLogger.LoggerFactory; }
            //logger = loggerFactory.CreateLogger<T>();

            var sec = new CodeSectionScope(logger, typeof(T), name, payload, TraceLogger.TraceSource, sourceLevel, logLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);
            var stopTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var delta = stopTicks - startTicks;
            return sec;
        }
        public static CodeSectionScope BeginMethodScope(this IHost host, string name, Type t, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel logLevel = LogLevel.Debug, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            ILogger logger = null;
            if (host == null) { host = TraceLogger.Host; }

            //var loggerFactory = host?.Services?.GetRequiredService<ILoggerFactory>();
            //if (loggerFactory == null) { loggerFactory = TraceLogger.LoggerFactory; }

            if (host != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { t });
                logger = host.Services.GetRequiredService(loggerType) as ILogger;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var sec = new CodeSectionScope(logger, t, name, payload, TraceLogger.TraceSource, sourceLevel, logLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);
            var stopTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var delta = stopTicks - startTicks;
            return sec;
        }

        public static ILogger<T> GetLogger<T>(this IHost host)
        {
            if (host == null) return null;

            TraceLogger.Host = host;
            var logger = host.Services.GetRequiredService<ILogger<T>>();

            //TraceLogger.LoggerFactory = host?.Services?.GetRequiredService<ILoggerFactory>();
            //var logger = TraceLogger.LoggerFactory?.CreateLogger<T>();
            return logger;
        }
        public static void InitTraceLogger(this IHost host)
        {
            using (new SwitchOnDispose(TraceLogger._lockListenersNotifications, true))
            using (new SwitchOnDispose(TraceLogger._isInitializing, true))
            using (new SwitchOnDispose(TraceLogger._isInitializeComplete, false))
            {
                TraceLogger.Host = host;
                //TraceLogger.LoggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            }
            return;
        }

        //public static SectionScope BeginMethodScope(Type t, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel LogLevel = LogLevel.Trace, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //{
        //    var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

        //    var host = (App.Current as App).Host;
        //    var logger = host.Services.GetRequiredService<ILogger<MainWindow>>();
        //    ILogger logger

        //    var sec = new SectionScope(logger, typeof(T), null, payload, TraceLogger.TraceSource, sourceLevel, LogLevel.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);
        //    var stopTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //    var delta = stopTicks - startTicks;
        //    return sec;
        //}

        public static void LogDebug<T>(this ILogger<T> logger, object obj, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string membername = "", [CallerFilePath] string sourcefilepath = "", [CallerLineNumber] int sourcelinenumber = 0)
        {
            var startticks = TraceLogger.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Verbose, LogLevel.Debug, category, properties, source, startticks, membername, sourcefilepath, sourcelinenumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Debug(obj, category, properties, source);
        }
#if NET6_0_OR_GREATER
        public static void LogDebug<T>(this ILogger<T> logger, ref TraceLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0) // , "level"
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Verbose, LogLevel.Debug, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Debug(ref message, category, properties, source);
        }
        public static void LogDebug<T>(this ILogger<T> logger, string message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0) // , "level"
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Verbose, LogLevel.Debug, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Debug(message, category, properties, source);
        }
#else
        public static void LogDebug<T>(this ILogger<T> logger, NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Verbose, LogLevel.Debug, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Debug(message, category, properties, source);
        }
        public static void LogDebug<T>(this ILogger<T> logger, FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Verbose, LogLevel.Debug, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Debug(message, category, properties, source);
        }
#endif
        public static void LogDebug<T>(this ILogger<T> logger, Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Verbose, LogLevel.Debug, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Debug(getMessage, category, properties, source);
        }

#if NET6_0_OR_GREATER
        public static void LogInformation<T>(this ILogger<T> logger, ref TraceLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Information, LogLevel.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Information(ref message, category, properties, source);
        }
        public static void LogInformation<T>(this ILogger<T> logger, string message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Information, LogLevel.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Information(message, category, properties, source);
        }
#else
        public static void LogInformation<T>(this ILogger<T> logger, NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Information, LogLevel.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Information(message, category, properties, source);
        }
        public static void LogInformation<T>(this ILogger<T> logger, FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Information, LogLevel.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Information(message, category, properties, source);
        }
#endif
        public static void LogInformation<T>(this ILogger<T> logger, Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Information, LogLevel.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Information(getMessage, category, properties, source);
        }

#if NET6_0_OR_GREATER
        public static void LogWarning<T>(this ILogger<T> logger, ref TraceLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Warning, LogLevel.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Warning(ref message, category, properties, source);
        }
        public static void LogWarning<T>(this ILogger<T> logger, string message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Warning, LogLevel.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Warning(message, category, properties, source);
        }
#else
        public static void LogWarning<T>(this ILogger<T> logger, NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Warning, LogLevel.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Warning(message, category, properties, source);
        }
        public static void LogWarning<T>(this ILogger<T> logger, FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Warning, LogLevel.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Warning(message, category, properties, source);
        }
#endif
        public static void LogWarning<T>(this ILogger<T> logger, Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Warning, LogLevel.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Warning(getMessage, category, properties, source);
        }

#if NET6_0_OR_GREATER
        public static void LogError<T>(this ILogger<T> logger, ref TraceLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Error, LogLevel.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Error(ref message, category, properties, source);
        }
        public static void LogError<T>(this ILogger<T> logger, string message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Error, LogLevel.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Error(message, category, properties, source);
        }
#else
        public static void LogError<T>(this ILogger<T> logger, NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Error, LogLevel.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Error(message, category, properties, source);
        }
        public static void LogError<T>(this ILogger<T> logger, FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Error, LogLevel.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Error(message, category, properties, source);
        }
#endif
        public static void LogError<T>(this ILogger<T> logger, Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Error, LogLevel.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Error(getMessage, category, properties, source);
        }

        public static void LogException<T>(this ILogger<T> logger, Exception exception, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Error, LogLevel.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Exception(exception, category, properties, source);
        }

        public static ILoggingBuilder AddDiginsightFormatted(this ILoggingBuilder builder, ILoggerProvider logProvider, IConfiguration config = null, string configurationPrefix = null) // , IServiceProvider serviceProvider
        {
            TraceLogger.InitConfiguration(config);

            var traceLoggerFormatProvider = default(TraceLoggerFormatProvider);
            if (logProvider.GetType().Name == "Log4NetProvider") { traceLoggerFormatProvider = new DiginsightFormattedLog4NetProvider() { ConfigurationSuffix = configurationPrefix }; }
            else if (logProvider.GetType().Name == "ApplicationInsightsLoggerProvider") { traceLoggerFormatProvider = new DiginsightFormattedApplicationInsightsProvider() { ConfigurationSuffix = configurationPrefix }; }
            else if (logProvider.GetType().Name == "ConsoleProvider") { traceLoggerFormatProvider = new DiginsightFormattedConsoleProvider() { ConfigurationSuffix = configurationPrefix }; }
            else if (logProvider.GetType().Name == "DebugProvider") { traceLoggerFormatProvider = new DiginsightFormattedDebugProvider() { ConfigurationSuffix = configurationPrefix }; }
            else { traceLoggerFormatProvider = new TraceLoggerFormatProvider() { ConfigurationSuffix = configurationPrefix }; }

            traceLoggerFormatProvider.AddProvider(logProvider);

            builder.AddProvider(traceLoggerFormatProvider);
            return builder;
        }
        public static ILoggingBuilder AddDiginsightJson(this ILoggingBuilder builder, ILoggerProvider logProvider, IConfiguration config = null, string configurationPrefix = null) // , IServiceProvider serviceProvider
        {
            TraceLogger.InitConfiguration(config);

            var traceLoggerJsonProvider = default(TraceLoggerJsonProvider);
            if (logProvider.GetType().Name == "Log4NetProvider") { traceLoggerJsonProvider = new DiginsightJsonLog4NetProvider() { ConfigurationSuffix = configurationPrefix }; }
            else if (logProvider.GetType().Name == "ApplicationInsightsLoggerProvider") { traceLoggerJsonProvider = new DiginsightJsonApplicationInsightsProvider() { ConfigurationSuffix = configurationPrefix }; }
            else if (logProvider.GetType().Name == "ConsoleProvider") { traceLoggerJsonProvider = new DiginsightJsonConsoleProvider() { ConfigurationSuffix = configurationPrefix }; }
            else if (logProvider.GetType().Name == "DebugProvider") { traceLoggerJsonProvider = new DiginsightJsonDebugProvider() { ConfigurationSuffix = configurationPrefix }; }
            else { traceLoggerJsonProvider = new TraceLoggerJsonProvider() { ConfigurationSuffix = configurationPrefix }; }
            //TraceLoggerConsoleProvider, TraceLoggerDebugProvider
            traceLoggerJsonProvider.AddProvider(logProvider);

            builder.AddProvider(traceLoggerJsonProvider);
            return builder;
        }

        //public static ILoggingBuilder AddDiginsightApplicationInsight(this ILoggingBuilder builder, ILoggerProvider logProvider, IConfiguration config = null, string configurationPrefix = null) // , IServiceProvider serviceProvider
        //{
        //    TraceLogger.InitConfiguration(config);

        //    var traceLoggerProvider = new TraceLoggerJsonProvider() { ConfigurationSuffix = configurationPrefix };
        //    traceLoggerProvider.AddProvider(logProvider);

        //    builder.AddProvider(traceLoggerProvider);
        //    return builder;
        //}

    }
    public static class TraceLoggerFactoryExtensions
    {
        public static ILoggerFactory AddDiginsight(this ILoggerFactory factory, IServiceProvider serviceProvider, LogLevel minLevel)
        {
            return null;
        }
    }
}
