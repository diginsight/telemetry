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
        // public static ILoggerFactory LoggerFactory { get; set; }
        internal static Process CurrentProcess { get; set; }
        internal static SafeProcessHandle CurrentProcessSafeProcessHandle { get; set; }
        internal static Assembly EntryAssembly { get; set; }
        public static SystemDiagnosticsConfig Config { get; set; }
        public static string ProcessName = null;
        public static string EnvironmentName = null;
        public static int ProcessId = -1;
        public IList<ILogger> Listeners { get; } = new List<ILogger>();
        public ILoggerProvider Provider { get; set; }
        public static IFormatTraceEntry DefaultFormatTraceEntry { get; set; }
        public static IConfiguration Configuration { get; private set; }

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
                    TraceLogger.Configuration = configuration;
                    ConfigurationHelper.Init(configuration);

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

                if (TraceLogger.Host != null)
                {
                    Type loggerType = typeof(ILogger<>);
                    loggerType = loggerType.MakeGenericType(new[] { t });

                    var host = TraceLogger.Host;
                    logger = host.Services.GetRequiredService(loggerType) as ILogger;
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

            var sec = new CodeSectionScope(this, this.Name, null, null, TraceLogger.TraceSource, SourceLevels.Verbose, LogLevel.Debug, this.Name, null, source, startTicks, state?.ToString(), null, -1);
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
                var innerSectionScope = caller = caller != null ? caller.GetInnerSection() : new CodeSectionScope(this, this.Name, null, null, TraceLogger.TraceSource, SourceLevels.Verbose, LogLevel.Debug, this.Name, null, source, startTicks, "Unknown", null, -1, true) { IsInnerScope = true };

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

            var host = TraceLogger.Host;
            var logger = host.Services.GetRequiredService(loggerType) as ILogger;
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
            if (logger == null && TraceLogger.Host != null)
            {
                var host = TraceLogger.Host;
                logger = host.Services.GetRequiredService<ILogger<T>>();
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger<T>();
            }

            var sec = new CodeSectionScope(logger, typeof(T), null, payload, TraceLogger.TraceSource, sourceLevel, logLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);
            var stopTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var delta = stopTicks - startTicks;
            return sec;
        }
        public static CodeSectionScope BeginMethodScope(Type t, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel logLevel = LogLevel.Debug, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            ILogger logger = null;
            if (TraceLogger.Host != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { t });
                var host = TraceLogger.Host;
                logger = host.Services.GetRequiredService(loggerType) as ILogger;
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var sec = new CodeSectionScope(logger, t, null, payload, TraceLogger.TraceSource, sourceLevel, logLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);
            var stopTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var delta = stopTicks - startTicks;
            return sec;
        }

        public static CodeSectionScope BeginNamedScope<T>(string name = null, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel logLevel = LogLevel.Debug, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            ILogger<T> logger = null;
            if (logger == null && TraceLogger.Host != null)
            {
                var host = TraceLogger.Host;
                logger = host.Services.GetRequiredService<ILogger<T>>();
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger<T>();
            }

            var sec = new CodeSectionScope(logger, typeof(T), name, payload, TraceLogger.TraceSource, sourceLevel, logLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);
            var stopTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var delta = stopTicks - startTicks;
            return sec;
        }
        public static CodeSectionScope BeginNamedScope(Type t, string name = null, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel logLevel = LogLevel.Debug, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            ILogger logger = null;
            if (TraceLogger.Host != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { t });
                var host = TraceLogger.Host;
                logger = host.Services.GetRequiredService(loggerType) as ILogger;
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var sec = new CodeSectionScope(logger, t, name, payload, TraceLogger.TraceSource, sourceLevel, logLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);
            var stopTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var delta = stopTicks - startTicks;
            return sec;
        }

        public static void LogTrace(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Host != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var host = TraceLogger.Host;
                logger = host.Services.GetRequiredService(loggerType) as ILogger;
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Trace, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);

            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Trace(message, category, properties, source);
        }
        public static void LogTrace(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Host != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var host = TraceLogger.Host;
                logger = host.Services.GetRequiredService(loggerType) as ILogger;
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Trace, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Trace(message, category, properties, source);
        }
        public static void LogTrace(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Host != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var host = TraceLogger.Host;
                logger = host.Services.GetRequiredService(loggerType) as ILogger;
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Trace, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Trace(getMessage, category, properties, source);
        }

        public static void LogDebug(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Host != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var host = TraceLogger.Host;
                logger = host.Services.GetRequiredService(loggerType) as ILogger;
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Debug, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);

            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Debug(message, category, properties, source);
        }
        public static void LogDebug(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Host != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var host = TraceLogger.Host;
                logger = host.Services.GetRequiredService(loggerType) as ILogger;
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Debug, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Debug(message, category, properties, source);
        }
        public static void LogDebug(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Host != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var host = TraceLogger.Host;
                logger = host.Services.GetRequiredService(loggerType) as ILogger;
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Debug, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Debug(getMessage, category, properties, source);
        }

        public static void LogInformation(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Host != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var host = TraceLogger.Host;
                logger = host.Services.GetRequiredService(loggerType) as ILogger;
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);

            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Information(message, category, properties, source);
        }
        public static void LogInformation(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Host != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var host = TraceLogger.Host;
                logger = host.Services.GetRequiredService(loggerType) as ILogger;
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Information(message, category, properties, source);
        }
        public static void LogInformation(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Host != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var host = TraceLogger.Host;
                logger = host.Services.GetRequiredService(loggerType) as ILogger;
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Information(getMessage, category, properties, source);
        }

        public static void LogWarning(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Host != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var host = TraceLogger.Host;
                logger = host.Services.GetRequiredService(loggerType) as ILogger;
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);

            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Warning(message, category, properties, source);
        }
        public static void LogWarning(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Host != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var host = TraceLogger.Host;
                logger = host.Services.GetRequiredService(loggerType) as ILogger;
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Warning(message, category, properties, source);
        }
        public static void LogWarning(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Host != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var host = TraceLogger.Host;
                logger = host.Services.GetRequiredService(loggerType) as ILogger;
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Warning(getMessage, category, properties, source);
        }

        public static void LogError(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Host != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var host = TraceLogger.Host;
                logger = host.Services.GetRequiredService(loggerType) as ILogger;
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);

            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Error(message, category, properties, source);
        }
        public static void LogError(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Host != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var host = TraceLogger.Host;
                logger = host.Services.GetRequiredService(loggerType) as ILogger;
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Error(message, category, properties, source);
        }
        public static void LogError(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Host != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var host = TraceLogger.Host;
                logger = host.Services.GetRequiredService(loggerType) as ILogger;
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Error(getMessage, category, properties, source);
        }

        public static void LogException(Exception exception, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;

            ILogger logger = null;
            if (TraceLogger.Host != null)
            {
                Type loggerType = typeof(ILogger<>);
                loggerType = loggerType.MakeGenericType(new[] { type });
                var host = TraceLogger.Host;
                logger = host.Services.GetRequiredService(loggerType) as ILogger;
                //var loggerFactory = TraceLogger.LoggerFactory;
                //logger = loggerFactory.CreateLogger(loggerType);
            }

            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, type, null, null, null, SourceLevels.Verbose, LogLevel.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);

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

    public static class TraceLoggerExtensions
    {
        public static CodeSectionScope BeginMethodScope<T>(this ILogger<T> logger, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel logLevel = LogLevel.Debug, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
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
        public static void LogDebug<T>(this ILogger<T> logger, Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Verbose, LogLevel.Debug, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Debug(getMessage, category, properties, source);
        }

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
        public static void LogInformation<T>(this ILogger<T> logger, Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Information, LogLevel.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Information(getMessage, category, properties, source);
        }

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
        public static void LogWarning<T>(this ILogger<T> logger, Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionScope.Current.Value;
            var innerSectionScope = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSectionScope(logger, typeof(T), null, null, null, SourceLevels.Warning, LogLevel.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerSectionScope as ICodeSectionLogger;
            innerCodeSectionLogger.Warning(getMessage, category, properties, source);
        }

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
