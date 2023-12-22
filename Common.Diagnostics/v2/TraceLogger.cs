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

// TODO: options for final naming:
// . Diginsight.System.Diagnostics
// . Diginsight.System.Diagnostics.Log4Net
// . Diginsight.System.Diagnostics.Serilog
// . Diginsight.Dotnet.Logger
// . Diginsight.Dotnet.Xxxx
// Microsoft.Extensions.Logging.ExecutionFlow
// EFDF => TraceEntry
// Execution entry { MethodName, ClassName, TID   ,,,,,,,,, }

namespace Common
{
    public class TraceLogger : ILogger
    {
        #region internal state
        private static Type T = typeof(TraceLogger);
        private static readonly string _traceSourceName = "TraceSource";
        public string Name { get; set; }
        public static TraceSource TraceSource { get; set; }
        public static Stopwatch Stopwatch = TraceManager.Stopwatch;
        public static IHost Host { get; set; }
        public static IServiceProvider Services { get; set; }
        // public static ILoggerFactory LoggerFactory { get; set; }
        public static Process CurrentProcess { get; set; }
        internal static SafeProcessHandle CurrentProcessSafeProcessHandle { get; set; }
        internal static Assembly EntryAssembly { get; set; }
        public static ActivitySource ActivitySource = null;
        public static SystemDiagnosticsConfig Config { get; set; }
        public static string ProcessName = null;
        public static string EnvironmentName = null;
        public static int ProcessId = -1;
        public IList<ILogger> Listeners { get; } = new List<ILogger>();
        public ILoggerProvider Provider { get; set; }
        public static IFormatTraceEntry DefaultFormatTraceEntry { get; set; }
        public static IConfiguration Configuration { get; internal set; }

        internal static ConcurrentQueue<TraceEntry> _pendingEntries = new ConcurrentQueue<TraceEntry>();
        internal static Reference<bool> _lockListenersNotifications = new Reference<bool>(true);
        internal static Reference<bool> _isInitializing = new Reference<bool>(false);
        internal static Reference<bool> _isInitializeComplete = new Reference<bool>(false);
        #endregion

        #region .ctor
        static TraceLogger()
        {
            Stopwatch.Start();
            try
            {
                CurrentProcess = Process.GetCurrentProcess();
                ProcessName = CurrentProcess.ProcessName;
                ProcessId = CurrentProcess.Id;
                CurrentProcessSafeProcessHandle = CurrentProcess.SafeHandle;
            }
            catch (PlatformNotSupportedException)
            {
                ProcessName = "NO_PROCESS";
                ProcessId = -1;
            }

            EntryAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
            ActivitySource = new ActivitySource(EntryAssembly.GetName().Name); // , "1.0.0"
        }
        public TraceLogger(ILoggerProvider provider, string name)
        {
            this.Provider = provider;
            this.Name = name;
        }
        #endregion
        #region Init
        public static void InitConfiguration(IConfiguration configuration)
        {
            if (TraceLogger.Configuration != null) { return; }
            using (var sc = TraceLogger.BeginMethodScope(T))
            {
                try
                {
                    _lockListenersNotifications.PropertyChanged += _lockListenersNotifications_PropertyChanged;
                    if (configuration == null) { configuration = GetConfiguration(); }
                    TraceLogger.Configuration = TraceManager.Configuration = configuration;
                    //ConfigurationHelper.Init(configuration);

                    var traceLoggerFormatProvider = new TraceLoggerFormatProvider() { ConfigurationSuffix = "" };
                    traceLoggerFormatProvider.Initialize();
                    DefaultFormatTraceEntry = traceLoggerFormatProvider;
                }
                catch (Exception ex)
                {
                    var message = $"Exception '{ex.GetType().Name}' occurred: {ex.Message}\r\nAdditional Information:\r\n{ex}";
                    sc.LogException(new InvalidDataException(message, ex));
                    //Trace.WriteLine(message);
                }
            }
        }
        private static void _lockListenersNotifications_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var pendingEntries = new ConcurrentQueue<TraceEntry>();
            var pendingEntriesTemp = _pendingEntries;
            _pendingEntries = pendingEntries;

            pendingEntriesTemp.ForEach(entry =>
            {
                var traceSource = entry.TraceSource;

                var t = entry.CodeSectionBase?.T;
                ILogger logger = null;
                if (t == null) { return; }

                if (TraceLogger.Services != null)
                {
                    Type loggerType = typeof(ILogger<>);
                    loggerType = loggerType.MakeGenericType(new[] { t });

                    var services = TraceLogger.Services;
                    try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }

                    // var loggerFactory = TraceLogger.LoggerFactory;
                    // logger = loggerFactory.CreateLogger(loggerType);

                    logger.Log<TraceEntry>(entry.LogLevel, new EventId(0, entry.RequestContext?.RequestId), entry, entry.Exception, DefaultFormatTraceEntry.FormatTraceEntry); // formatTraceEntry
                }
            });
        }
        #endregion

        // ILogger
        public IDisposable BeginScope<TState>(TState state)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var classNameIndex = this.Name.LastIndexOf('.') + 1;
            var source = classNameIndex >= 0 ? this.Name.Substring(0, classNameIndex) : this.Name;
            if (source.EndsWith(".")) { source = source.TrimEnd('.'); }

            var sec = new CodeSectionScope(null, this, this.Name, null, null, TraceLogger.TraceSource, SourceLevels.Verbose, LogLevel.Debug, this.Name, null, source, startTicks, state?.ToString(), null, -1);
            return sec;
        }
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var entry = default(TraceEntry);
            if (state is TraceEntry e)
            {
                entry = e;
                if (entry.Category == null) { entry.Category = this.Name; }
            }
            else
            {
                var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
                var type = typeof(InternalClass);
                var caller = CodeSectionBase.Current.Value;

                var classNameIndex = this.Name.LastIndexOf('.') + 1;
                var source = classNameIndex >= 0 ? this.Name.Substring(0, classNameIndex) : this.Name;
                if (source.EndsWith(".")) { source = source.TrimEnd('.'); }
                var innerSectionScope = caller = caller != null ? caller.GetInnerSection() : new CodeSectionScope(null, this, this.Name, null, null, TraceLogger.TraceSource, SourceLevels.Verbose, LogLevel.Debug, this.Name, null, source, startTicks, "Unknown", null, -1, true) { IsInnerScope = true };

                if (innerSectionScope?._isLogEnabled == false) { return; }
                if (logLevel  < innerSectionScope.MinimumLogLevel) { return; }

                var stateFormatter = formatter != null ? formatter : (s, exc) => { return s.GetLogString(); };
                var traceEventType = LogLevelHelper.ToTraceEventType(logLevel);
                var sourceLevel = LogLevelHelper.ToSourceLevel(logLevel);

                entry = new TraceEntry()
                {
                    GetMessage = () => { return stateFormatter(state, null); },
                    TraceEventType = traceEventType,
                    SourceLevel = sourceLevel,
                    LogLevel = logLevel,
                    Properties = null,
                    Source = source,
                    Category = this.Name,
                    CodeSectionBase = innerSectionScope,
                    Thread = Thread.CurrentThread,
                    ThreadID = Thread.CurrentThread.ManagedThreadId,
                    ApartmentState = Thread.CurrentThread.GetApartmentState(),
                    DisableCRLFReplace = false,
                    ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds,
                    TraceStartTicks = startTicks
                };
            }

            if (this.Listeners != null && this.Listeners.Count > 0)
            {
                foreach (var listener in this.Listeners)
                {
                    try
                    {
                        if (!TraceLogger._lockListenersNotifications.Value)
                        {
                            //if (_logger == null) { _logger = GetEntrylogger(ref entry); }

                            IFormatTraceEntry iFormatTraceEntry = Provider as IFormatTraceEntry;
                            Func<TraceEntry, Exception, string> formatTraceEntry = iFormatTraceEntry != null ? (Func<TraceEntry, Exception, string>)iFormatTraceEntry.FormatTraceEntry : null;
                            listener.Log(logLevel, eventId, entry, exception ?? entry.Exception, formatTraceEntry);
                        }
                        else
                        {
                            TraceLogger._pendingEntries.Enqueue(entry);
                            // if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.InitConfiguration(null); }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }

            return;
        }

        #region GetEntrylogger
        private static ILogger GetEntrylogger(ref TraceEntry entry)
        {
            var t = entry.CodeSectionBase?.T;
            Type loggerType = typeof(ILogger<>);
            loggerType = loggerType.MakeGenericType(new[] { t });

            var services = TraceLogger.Services;
            var logger = default(ILogger);
            try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
            //var loggerFactory = TraceLogger.LoggerFactory;
            //var logger = loggerFactory.CreateLogger(loggerType);

            return logger;
        }
        #endregion

        // helpers
        public static CodeSectionScope BeginMethodScope<T>(object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel logLevel = LogLevel.Debug, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            ILogger<T> logger = null;
            var services = TraceLogger.Services;
            if (logger == null && services != null)
            {
                logger = services.GetService<ILogger<T>>();
            }
            //var traceLoggerMinimumLevelService = default(ITraceLoggerMinimumLevel);
            //if (host != null)
            //{
            //    traceLoggerMinimumLevelService = services?.GetService<ITraceLoggerMinimumLevel>();
            //}

            var sec = new CodeSectionScope(null, logger, typeof(T), null, payload, TraceLogger.TraceSource, sourceLevel, logLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);
            var stopTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var delta = stopTicks - startTicks;
            return sec;
        }
        public static CodeSectionScope BeginMethodScope(Type t, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel logLevel = LogLevel.Debug, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            ILogger logger = null;
            var services = TraceLogger.Services;
            if (services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { t });
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }
            //var traceLoggerMinimumLevelService = default(ITraceLoggerMinimumLevel);
            //if (host != null)
            //{
            //    traceLoggerMinimumLevelService = services?.GetService<ITraceLoggerMinimumLevel>();
            //}

            var sec = new CodeSectionScope(null, logger, t, null, payload, TraceLogger.TraceSource, sourceLevel, logLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);
            var stopTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var delta = stopTicks - startTicks;
            return sec;
        }

        public static CodeSectionScope BeginNamedScope<T>(string name = null, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel logLevel = LogLevel.Debug, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            ILogger<T> logger = null;
            var services = TraceLogger.Services;
            if (logger == null && services != null)
            {
                logger = services.GetService<ILogger<T>>();
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger<T>();
            }
            //var traceLoggerMinimumLevelService = default(ITraceLoggerMinimumLevel);
            //if (host != null)
            //{
            //    traceLoggerMinimumLevelService = services?.GetService<ITraceLoggerMinimumLevel>();
            //}

            var sec = new CodeSectionScope(null, logger, typeof(T), name, payload, TraceLogger.TraceSource, sourceLevel, logLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);
            var stopTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var delta = stopTicks - startTicks;
            return sec;
        }
        public static CodeSectionScope BeginNamedScope(Type t, string name = null, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel logLevel = LogLevel.Debug, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            ILogger logger = null;
            var services = TraceLogger.Services;
            if (services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { t });
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }
            //var traceLoggerMinimumLevelService = default(ITraceLoggerMinimumLevel);
            //if (host != null)
            //{
            //    traceLoggerMinimumLevelService = services?.GetService<ITraceLoggerMinimumLevel>();
            //}

            var sec = new CodeSectionScope(null, logger, t, name, payload, TraceLogger.TraceSource, sourceLevel, logLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);
            var stopTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var delta = stopTicks - startTicks;
            return sec;
        }

#if NET6_0_OR_GREATER
        public static void LogTrace(ref TraceLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Trace, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);

            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Trace(ref message, category, properties, source);
        }
        public static void LogTrace(string message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Trace, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);

            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Trace(message, category, properties, source);
        }
#else
        public static void LogTrace(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Trace, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);

            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Trace(message, category, properties, source);
        }
        public static void LogTrace(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Trace, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Trace(message, category, properties, source);
        }
#endif
        public static void LogTrace(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Trace, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Trace(getMessage, category, properties, source);
        }

#if NET6_0_OR_GREATER
        public static void LogDebug(ref DebugLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Debug, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);

            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Debug(ref message, category, properties, source);
        }
        public static void LogDebug(string message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Debug, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);

            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Debug(message, category, properties, source);
        }
#else
        public static void LogDebug(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Debug, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);

            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Debug(message, category, properties, source);
        }
        public static void LogDebug(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Debug, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Debug(message, category, properties, source);
        }
#endif
        public static void LogDebug(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Debug, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Debug(getMessage, category, properties, source);
        }

#if NET6_0_OR_GREATER
        public static void LogInformation(ref InformationLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);

            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Information(ref message, category, properties, source);
        }
        public static void LogInformation(string message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);

            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Information(message, category, properties, source);
        }
#else
        public static void LogInformation(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);

            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Information(message, category, properties, source);
        }
        public static void LogInformation(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Information(message, category, properties, source);
        }
#endif
        public static void LogInformation(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Information(getMessage, category, properties, source);
        }

#if NET6_0_OR_GREATER
        public static void LogWarning(ref WarningLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);

            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Warning(ref message, category, properties, source);
        }
        public static void LogWarning(string message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);

            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Warning(message, category, properties, source);
        }
#else
        public static void LogWarning(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);

            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Warning(message, category, properties, source);
        }
        public static void LogWarning(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Warning(message, category, properties, source);
        }
#endif
        public static void LogWarning(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Warning(getMessage, category, properties, source);
        }

#if NET6_0_OR_GREATER
        public static void LogError(ref ErrorLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);

            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Error(ref message, category, properties, source);
        }
        public static void LogError(string message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);

            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Error(message, category, properties, source);
        }
#else
        public static void LogError(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);

            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Error(message, category, properties, source);
        }
        public static void LogError(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Error(message, category, properties, source);
        }
#endif
        public static void LogError(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Error(getMessage, category, properties, source);
        }

        public static void LogException(Exception exception, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Services != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var services = TraceLogger.Services;
                try { logger = services.GetService(loggerType) as ILogger; } catch (Exception _) { }
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(null, logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);

            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Exception(exception, category, properties, source);
        }

        public static IConfiguration GetConfiguration()
        {
            IConfiguration configuration = null;
            var jsonFileName = "appsettings";
            var currentDirectory = Directory.GetCurrentDirectory();
            var appdomainFolder = System.AppDomain.CurrentDomain.BaseDirectory.Trim('\\');

            var environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLower();
            if (string.IsNullOrEmpty(environment)) { environment = System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")?.ToLower(); }
            if (string.IsNullOrEmpty(environment)) { environment = System.Environment.GetEnvironmentVariable("ENVIRONMENT")?.ToLower(); }
            if (string.IsNullOrEmpty(environment)) { environment = "production"; }

            var jsonFile = currentDirectory == appdomainFolder ? $"{jsonFileName}.json" : Path.Combine(appdomainFolder, $"{jsonFileName}.json");
            var builder = default(IConfigurationBuilder);
            DebugHelper.IfDebug(() =>
            {   // for debug build only check environment setting on appsettings.json
                builder = new ConfigurationBuilder()
                              .AddJsonFile(jsonFile, true, true)
                              .AddInMemoryCollection();

                builder.AddEnvironmentVariables();
                configuration = builder.Build();
                var jsonEnvironment = configuration.GetValue($"AppSettings:Environment", "");
                if (string.IsNullOrEmpty(jsonEnvironment)) { environment = jsonEnvironment; }
            });

            var environmentJsonFile = currentDirectory == appdomainFolder ? $"{jsonFileName}.json" : Path.Combine(appdomainFolder, $"{jsonFileName}.{environment}.json");
            builder = new ConfigurationBuilder();
            builder.AddJsonFile(jsonFile, true, true);
            if (File.Exists(environmentJsonFile)) { builder = builder.AddJsonFile(environmentJsonFile, true, true); }
            builder = builder.AddInMemoryCollection();
            builder.AddEnvironmentVariables();
            configuration = builder.Build();

            TraceLogger.EnvironmentName = environment;

            return configuration;
        }
        public static string GetMethodName([CallerMemberName] string memberName = "") { return memberName; }
    }
}
