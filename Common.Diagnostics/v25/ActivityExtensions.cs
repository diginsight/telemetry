#region using
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.VisualBasic;
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
    public static partial class ActivityExtensions
    {
        public static CodeSectionScope StartMethodActivity<T>(this ActivitySource activitySource, ILogger<T> logger, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel logLevel = LogLevel.Debug, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var services = TraceLogger.Services;
            if (logger == null && services != null) { try { logger = services?.GetService<ILogger<T>>(); } catch (Exception _) { } }

            var callerType = typeof(T);
            var scope = new CodeSectionScope(activitySource, logger, callerType, null, payload, TraceLogger.TraceSource, sourceLevel, logLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);

            // var stopTicks = TraceLogger.Stopwatch.ElapsedTicks;
            // var delta = stopTicks - startTicks;
            return scope;
        }
        //public static CodeSectionScope StartMethodActivity<T>(this IHost host, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel logLevel = LogLevel.Debug, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //{
        //    var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

        //    ILogger<T> logger = null;
        //    if (host == null) { host = TraceLogger.Host; }
        //    if (host != null) { try { logger = services?.GetService<ILogger<T>>(); } catch (Exception _) { } }

        //    //var loggerFactory = services.GetService<ILoggerFactory>();
        //    //if (loggerFactory == null) { loggerFactory = TraceLogger.LoggerFactory; }
        //    //logger = loggerFactory.CreateLogger<T>();

        //    var callerType = typeof(T);
        //    var scope = new CodeSectionScope(activitySource, logger, traceLoggerMinimumLevelService, callerType, null, payload, TraceLogger.TraceSource, sourceLevel, logLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);

        //    //var stopTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //    //var delta = stopTicks - startTicks;
        //    return scope;
        //}

        //public static CodeSectionScope StartMethodActivity(this IHost host, Type callerType, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel logLevel = LogLevel.Debug, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //{
        //    var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

        //    var traceLoggerMinimumLevelService = default(ITraceLoggerMinimumLevel);
        //    ILogger logger = null;
        //    if (host == null) { host = TraceLogger.Host; }
        //    if (host != null)
        //    {
        //        Type loggerType = typeof(ILogger<>);
        //        loggerType = loggerType.MakeGenericType(new[] { callerType });
        //        logger = services.GetService(loggerType) as ILogger;
        //    }

        //    var scope = new CodeSectionScope(activitySource, logger, traceLoggerMinimumLevelService, callerType, null, payload, TraceLogger.TraceSource, sourceLevel, logLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);

        //    //var stopTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //    //var delta = stopTicks - startTicks;
        //    return scope;
        //}

        public static CodeSectionScope StartNamedActivity<T>(this ActivitySource activitySource, ILogger<T> logger, string name, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel logLevel = LogLevel.Debug, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var services = TraceLogger.Services;
            if (logger == null && services != null) { try { logger = services?.GetService<ILogger<T>>(); } catch (Exception _) { } }

            var callerType = typeof(T);
            var scope = new CodeSectionScope(activitySource, logger, callerType, name, payload, TraceLogger.TraceSource, sourceLevel, logLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);

            //var fullCallerMemberName = !string.IsNullOrEmpty(scope.Name) ? $"{scope.MemberName}.{scope.Name}" : scope.MemberName;

            // var stopTicks = TraceLogger.Stopwatch.ElapsedTicks;
            // var delta = stopTicks - startTicks;
            return scope;
        }
        //public static CodeSectionScope StartNamedActivity<T>(this IHost host, string name, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel logLevel = LogLevel.Debug, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //{
        //    var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

        //    ILogger<T> logger = null;
        //    if (host == null) { host = TraceLogger.Host; }
        //    if (host != null) { try { logger = services?.GetService<ILogger<T>>(); } catch (Exception _) { } }

        //    var callerType = typeof(T);
        //    var scope = new CodeSectionScope(activitySource, logger, callerType, name, payload, TraceLogger.TraceSource, sourceLevel, logLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);

        //    //var stopTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //    //var delta = stopTicks - startTicks;
        //    return scope;
        //}
        //public static CodeSectionScope StartNamedActivity(this IHost host, string name, Type callerType, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel logLevel = LogLevel.Debug, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //{
        //    var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

        //    var traceLoggerMinimumLevelService = default(ITraceLoggerMinimumLevel);
        //    ILogger logger = null;
        //    if (host == null) { host = TraceLogger.Host; }
        //    if (host != null)
        //    {
        //        Type loggerType = typeof(ILogger<>);
        //        loggerType = loggerType.MakeGenericType(new[] { callerType });
        //        logger = services.GetService(loggerType) as ILogger;
        //    }

        //    var scope = new CodeSectionScope(activitySource, logger, callerType, name, payload, TraceLogger.TraceSource, sourceLevel, logLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);

        //    //var stopTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //    //var delta = stopTicks - startTicks;
        //    return scope;
        //}

        public static ILogger<T> GetLogger<T>(this IHost host)
        {
            if (host == null) return null;

            TraceLogger.Services = host?.Services;
            var logger = TraceLogger.Services.GetService<ILogger<T>>();

            //TraceLogger.LoggerFactory = host?.Services?.GetService<ILoggerFactory>();
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
                TraceLogger.Services = host.Services;
                //TraceLogger.LoggerFactory = host.Services.GetService<ILoggerFactory>();
            }
            return;
        }
        public static void InitTraceLogger(this IServiceProvider services)
        {
            using (new SwitchOnDispose(TraceLogger._lockListenersNotifications, true))
            using (new SwitchOnDispose(TraceLogger._isInitializing, true))
            using (new SwitchOnDispose(TraceLogger._isInitializeComplete, false))
            {
                TraceLogger.Host = null;
                TraceLogger.Services = services;
                //TraceLogger.LoggerFactory = host.Services.GetService<ILoggerFactory>();

            }
            return;
        }

        //        public static void LogDebug<T>(this Activity activity, ILogger<T> logger, object obj, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string membername = "", [CallerFilePath] string sourcefilepath = "", [CallerLineNumber] int sourcelinenumber = 0)
        //        {
        //            var startticks = TraceLogger.Stopwatch.ElapsedTicks;
        //            //var type = typeof(InternalClass);
        //            var caller = CodeSectionScope.Current.Value;
        //            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(activitySource, logger, typeof(T), null, null, null, SourceLevels.Verbose, LogLevel.Debug, category, properties, source, startticks, membername, sourcefilepath, sourcelinenumber, true);
        //            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
        //            innerCodeSectionLogger.Debug(obj, category, properties, source);
        //        }
        //#if NET6_0_OR_GREATER
        //        public static void LogDebug<T>(this Activity activity, ILogger<T> logger, [InterpolatedStringHandlerArgument("logger")] ref DebugLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0) // , "level"
        //        {
        //            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //            var caller = CodeSectionScope.Current.Value;
        //            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(activitySource, logger, typeof(T), null, null, null, SourceLevels.Verbose, LogLevel.Debug, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
        //            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
        //            innerCodeSectionLogger.Debug(ref message, category, properties, source);
        //        }
        //        public static void LogDebug<T>(this Activity activity, ILogger<T> logger, string message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0) // , "level"
        //        {
        //            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //            var caller = CodeSectionScope.Current.Value;
        //            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(activitySource, logger, typeof(T), null, null, null, SourceLevels.Verbose, LogLevel.Debug, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
        //            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
        //            innerCodeSectionLogger.Debug(message, category, properties, source);
        //        }
        //#else
        //        public static void LogDebug<T>(this Activity activity, ILogger<T> logger, NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //        {
        //            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //            var type = typeof(InternalClass);
        //            var caller = CodeSectionScope.Current.Value;
        //            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(activitySource, logger, typeof(T), null, null, null, SourceLevels.Verbose, LogLevel.Debug, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
        //            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
        //            innerCodeSectionLogger.Debug(message, category, properties, source);
        //        }
        //        public static void LogDebug<T>(this Activity activity, ILogger<T> logger, FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //        {
        //            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //            var type = typeof(InternalClass);
        //            var caller = CodeSectionScope.Current.Value;
        //            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(activitySource, logger, typeof(T), null, null, null, SourceLevels.Verbose, LogLevel.Debug, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
        //            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
        //            innerCodeSectionLogger.Debug(message, category, properties, source);
        //        }
        //#endif
        //        public static void LogDebug<T>(this Activity activity, ILogger<T> logger, Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //        {
        //            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //            //var type = typeof(InternalClass);
        //            var caller = CodeSectionScope.Current.Value;
        //            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(activitySource, logger, typeof(T), null, null, null, SourceLevels.Verbose, LogLevel.Debug, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
        //            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
        //            innerCodeSectionLogger.Debug(getMessage, category, properties, source);
        //        }

        //#if NET6_0_OR_GREATER
        //        public static void LogInformation<T>(this Activity activity, ILogger<T> logger, [InterpolatedStringHandlerArgument("logger")] ref InformationLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //        {
        //            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //            var caller = CodeSectionScope.Current.Value;
        //            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(activitySource, logger, typeof(T), null, null, null, SourceLevels.Information, LogLevel.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
        //            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
        //            innerCodeSectionLogger.Information(ref message, category, properties, source);
        //        }
        //        public static void LogInformation<T>(this Activity activity, ILogger<T> logger, string message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //        {
        //            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //            var caller = CodeSectionScope.Current.Value;
        //            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(activitySource, logger, typeof(T), null, null, null, SourceLevels.Information, LogLevel.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
        //            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
        //            innerCodeSectionLogger.Information(message, category, properties, source);
        //        }
        //#else
        //        public static void LogInformation<T>(this Activity activity, ILogger<T> logger, NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //        {
        //            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //            var type = typeof(InternalClass);
        //            var caller = CodeSectionScope.Current.Value;
        //            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(activitySource, logger, typeof(T), null, null, null, SourceLevels.Information, LogLevel.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
        //            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
        //            innerCodeSectionLogger.Information(message, category, properties, source);
        //        }
        //        public static void LogInformation<T>(this Activity activity, ILogger<T> logger, FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //        {
        //            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //            var type = typeof(InternalClass);
        //            var caller = CodeSectionScope.Current.Value;
        //            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(activitySource, logger, typeof(T), null, null, null, SourceLevels.Information, LogLevel.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
        //            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
        //            innerCodeSectionLogger.Information(message, category, properties, source);
        //        }
        //#endif
        //        public static void LogInformation<T>(this Activity activity, ILogger<T> logger, Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //        {
        //            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //            //var type = typeof(InternalClass);
        //            var caller = CodeSectionScope.Current.Value;
        //            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(activitySource, logger, typeof(T), null, null, null, SourceLevels.Information, LogLevel.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
        //            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
        //            innerCodeSectionLogger.Information(getMessage, category, properties, source);
        //        }

        //#if NET6_0_OR_GREATER
        //        public static void LogWarning<T>(this Activity activity, ILogger<T> logger, [InterpolatedStringHandlerArgument("logger")] ref WarningLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //        {
        //            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //            var caller = CodeSectionScope.Current.Value;
        //            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(activitySource, logger, typeof(T), null, null, null, SourceLevels.Warning, LogLevel.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
        //            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
        //            innerCodeSectionLogger.Warning(ref message, category, properties, source);
        //        }
        //        public static void LogWarning<T>(this Activity activity, ILogger<T> logger, string message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //        {
        //            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //            var caller = CodeSectionScope.Current.Value;
        //            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(activitySource, logger, typeof(T), null, null, null, SourceLevels.Warning, LogLevel.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
        //            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
        //            innerCodeSectionLogger.Warning(message, category, properties, source);
        //        }
        //#else
        //        public static void LogWarning<T>(this Activity activity, ILogger<T> logger, NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //        {
        //            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //            var type = typeof(InternalClass);
        //            var caller = CodeSectionScope.Current.Value;
        //            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(activitySource, logger, typeof(T), null, null, null, SourceLevels.Warning, LogLevel.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
        //            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
        //            innerCodeSectionLogger.Warning(message, category, properties, source);
        //        }
        //        public static void LogWarning<T>(this Activity activity, ILogger<T> logger, FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //        {
        //            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //            var type = typeof(InternalClass);
        //            var caller = CodeSectionScope.Current.Value;
        //            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(activitySource, logger, typeof(T), null, null, null, SourceLevels.Warning, LogLevel.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
        //            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
        //            innerCodeSectionLogger.Warning(message, category, properties, source);
        //        }
        //#endif
        //        public static void LogWarning<T>(this Activity activity, ILogger<T> logger, Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //        {
        //            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //            //var type = typeof(InternalClass);
        //            var caller = CodeSectionScope.Current.Value;
        //            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(activitySource, logger, typeof(T), null, null, null, SourceLevels.Warning, LogLevel.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
        //            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
        //            innerCodeSectionLogger.Warning(getMessage, category, properties, source);
        //        }

        //#if NET6_0_OR_GREATER
        //        public static void LogError<T>(this Activity activity, ILogger<T> logger, [InterpolatedStringHandlerArgument("logger")] ref ErrorLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //        {
        //            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //            var caller = CodeSectionScope.Current.Value;
        //            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(activitySource, logger, typeof(T), null, null, null, SourceLevels.Error, LogLevel.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
        //            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
        //            innerCodeSectionLogger.Error(ref message, category, properties, source);
        //        }
        //        public static void LogError<T>(this Activity activity, ILogger<T> logger, string message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //        {
        //            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //            var caller = CodeSectionScope.Current.Value;
        //            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(activitySource, logger, typeof(T), null, null, null, SourceLevels.Error, LogLevel.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
        //            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
        //            innerCodeSectionLogger.Error(message, category, properties, source);
        //        }
        //#else
        //        public static void LogError<T>(this Activity activity, ILogger<T> logger, NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //        {
        //            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //            var type = typeof(InternalClass);
        //            var caller = CodeSectionScope.Current.Value;
        //            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(activitySource, logger, typeof(T), null, null, null, SourceLevels.Error, LogLevel.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
        //            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
        //            innerCodeSectionLogger.Error(message, category, properties, source);
        //        }
        //        public static void LogError<T>(this Activity activity, ILogger<T> logger, FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //        {
        //            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //            var type = typeof(InternalClass);
        //            var caller = CodeSectionScope.Current.Value;
        //            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(activitySource, logger, typeof(T), null, null, null, SourceLevels.Error, LogLevel.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
        //            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
        //            innerCodeSectionLogger.Error(message, category, properties, source);
        //        }
        //#endif
        //        public static void LogError<T>(this Activity activity, ILogger<T> logger, Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //        {
        //            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //            //var type = typeof(InternalClass);
        //            var caller = CodeSectionScope.Current.Value;
        //            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(activitySource, logger, typeof(T), null, null, null, SourceLevels.Error, LogLevel.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
        //            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
        //            innerCodeSectionLogger.Error(getMessage, category, properties, source);
        //        }

        //        public static void LogException<T>(this Activity activity, ILogger<T> logger, Exception exception, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        //        {
        //            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
        //            //var type = typeof(InternalClass);
        //            var caller = CodeSectionScope.Current.Value;
        //            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(activitySource, logger, typeof(T), null, null, null, SourceLevels.Error, LogLevel.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
        //            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
        //            innerCodeSectionLogger.Exception(exception, category, properties, source);
        //        }

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
    //public static class TraceLoggerFactoryExtensions
    //{
    //    public static ILoggerFactory AddDiginsight(this ILoggerFactory factory, IServiceProvider serviceProvider, LogLevel minLevel)
    //    {
    //        return null;
    //    }
    //}
}
