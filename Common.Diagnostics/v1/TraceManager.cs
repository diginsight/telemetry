#region using
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Threading;
using System.Windows;
//using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Json.Serialization;
#endregion

namespace Common
{
    public static class TraceManagerExtensions
    {
        public static CodeSection GetCodeSection<T>(this T pthis, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var sec = new CodeSection(typeof(T), null, payload, TraceManager.TraceSource, sourceLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);
            //var stopTicks = TraceManager.Stopwatch.ElapsedTicks;
            //var delta = stopTicks - startTicks;
            return sec;
        }
        public static CodeSection GetNamedSection<T>(this T pthis, string name = null, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var sec = new CodeSection(typeof(T), name, payload, TraceManager.TraceSource, sourceLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);
            return sec;
        }
        public static CodeSection GetNamedSection<T>(string name = null, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var sec = new CodeSection(typeof(T), name, payload, TraceManager.TraceSource, sourceLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);
            return sec;
        }
    }
    public class TraceManager
    {
        #region const
        public const string CONFIGSETTING_MAXMESSAGELEVEL = "MaxMessageLevel"; public const int CONFIGDEFAULT_MAXMESSAGELEVEL = 3;
        public const string CONFIGSETTING_MAXMESSAGELEN = "MaxMessageLen"; public const int CONFIGDEFAULT_MAXMESSAGELEN = 1024;
        public const string CONFIGSETTING_MAXMESSAGELENINFO = "MaxMessageLenInfo"; public const int CONFIGDEFAULT_MAXMESSAGELENINFO = 1024;
        public const string CONFIGSETTING_MAXMESSAGELENWARNING = "MaxMessageLenWarning"; public const int CONFIGDEFAULT_MAXMESSAGELENWARNING = 1024;
        public const string CONFIGSETTING_MAXMESSAGELENERROR = "MaxMessageLenError"; public const int CONFIGDEFAULT_MAXMESSAGELENERROR = -1;
        public const string CONFIGSETTING_DEFAULTLISTENERTYPENAME = "Common.TraceListenerDefault,Common.Diagnostics";
        #endregion
        #region internal state
        private static Type T = typeof(TraceManager);
        private static ClassConfigurationGetter<TraceManager> classConfigurationGetter;
        private static readonly string _traceSourceName = "TraceSource";
        public static Func<string, string> CRLF2Space = (string s) => { return s?.Replace("\r", " ")?.Replace("\n", " "); };
        public static Func<string, string> CRLF2Encode = (string s) => { return s?.Replace("\r", "\\r")?.Replace("\n", "\\n"); };
        public static IConfiguration Configuration { get; internal set; }
        public static ConcurrentDictionary<string, object> Properties { get; set; } = new ConcurrentDictionary<string, object>();
        public static ConcurrentDictionary<Assembly, IModuleContext> Modules { get; set; } = new ConcurrentDictionary<Assembly, IModuleContext>();
        public static TraceSource TraceSource { get; set; }
        public static SystemDiagnosticsConfig Config { get; set; }
        public static Stopwatch Stopwatch = new Stopwatch();
        internal static Process CurrentProcess { get; set; }

        internal static Assembly EntryAssembly { get; set; }
        public static string ProcessName = null;
        public static string EnvironmentName = null;
        public static int ProcessId = -1;
        internal static Reference<bool> _lockListenersNotifications = new Reference<bool>(true);
        internal static Reference<int> _isInitializing = new Reference<int>(0);
        internal static Reference<bool> _isInitializeComplete = new Reference<bool>(false);
        internal static ConcurrentQueue<TraceEntry> _pendingEntries = new ConcurrentQueue<TraceEntry>();
        #endregion

        #region .ctor
        static TraceManager()
        {
            _lockListenersNotifications.PropertyChanged += _lockListenersNotifications_PropertyChanged;

            Stopwatch.Start();
            try
            {
                CurrentProcess = Process.GetCurrentProcess();
                ProcessName = CurrentProcess.ProcessName;
                ProcessId = CurrentProcess.Id;
            }
            catch (PlatformNotSupportedException)
            {
                ProcessName = "NO_PROCESS";
                ProcessId = -1;
            }

            EntryAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
        }
        #endregion

        #region Init
        private static void _lockListenersNotifications_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var pendingEntries = new ConcurrentQueue<TraceEntry>();
            var pendingEntriesTemp = _pendingEntries;
            _pendingEntries = pendingEntries;

            pendingEntriesTemp.ForEach(entry =>
            {
                var traceSource = entry.TraceSource;

                if (traceSource?.Listeners != null)
                {
                    var tracesourcelisteners = traceSource?.Listeners?.OfType<TraceListener>()?.ToList();
                    foreach (TraceListener listener in tracesourcelisteners) { try { listener.WriteLine(entry); } catch (Exception) { } }
                }

                if (System.Diagnostics.Trace.Listeners != null)
                {
                    var traceListeners = System.Diagnostics.Trace.Listeners?.OfType<TraceListener>()?.ToList();
                    foreach (TraceListener listener in traceListeners) { try { listener.WriteLine(entry); } catch (Exception) { } }
                }
            });
        }
        public static void Init(SourceLevels filterLevel, IConfiguration configuration)
        {
            var tid = Thread.CurrentThread.ManagedThreadId;
            using (new SwitchOnDispose(_lockListenersNotifications, true))
            using (new SwitchOnDispose<int>(_isInitializing, (s) => { if (s.Value == 0) { s.Value = tid; } }, (s) => { if (s.Value == tid) { s.Value = 0; } }))
            using (new SwitchOnDispose(_isInitializeComplete, false))
            {
                using (var sec = TraceManager.GetCodeSection(T))
                {
                    try
                    {
                        if (_isInitializing.Value != tid) { ThreadHelper.WaitUntil(() => _isInitializing.Value == 0); return; }
                        if (TraceManager.Configuration != null) { return; }

                        if (configuration == null) { configuration = GetConfiguration(); }
                        TraceLogger.Configuration = TraceManager.Configuration = configuration;
                        // ConfigurationHelper.Init(configuration);
                        if (classConfigurationGetter == null) { classConfigurationGetter = new ClassConfigurationGetter<TraceManager>(TraceManager.Configuration); }

                        var defaultConfig = new ListenerConfig()
                        {
                            name = "Default",
                            action = "add",
                            type = "Common.TraceListenerFormatItems, Common.Diagnostics",
                            innerListener = new ListenerConfig()
                            {
                                name = "Default",
                                action = "removeOrAdd",
                                type = "System.Diagnostics.DefaultTraceListener, System.Diagnostics.TraceSource"
                            }
                        };
                        if (_isInitializing.Value != tid) { ThreadHelper.WaitUntil(() => _isInitializing.Value == 0); return; }
                        ApplyListenerConfig(defaultConfig, System.Diagnostics.Trace.Listeners);

                        var systemDiagnosticsConfig = new SystemDiagnosticsConfig();
                        configuration.GetSection("system.diagnostics").Bind(systemDiagnosticsConfig);
                        Config = systemDiagnosticsConfig;

                        var sourceConfig = systemDiagnosticsConfig?.sources?.FirstOrDefault(s => s.name == _traceSourceName);
                        var switchName = sourceConfig?.switchName;
                        var switchConfig = systemDiagnosticsConfig?.switches?.FirstOrDefault(sw => sw.name == switchName);

                        var sourceLevel = switchConfig != null ? switchConfig.value : SourceLevels.All;
                        TraceSource = new TraceSource(_traceSourceName, sourceLevel);

                        if (_isInitializing.Value != tid) { ThreadHelper.WaitUntil(() => _isInitializing.Value == 0); return; }
                        TraceSource.Listeners.Clear();

                        sourceConfig?.listeners?.ForEach(lc =>
                        {
                            ApplyListenerConfig(lc, TraceSource.Listeners);
                        });
                        Config?.sharedListeners?.ForEach(lc =>
                        {
                            ApplyListenerConfig(lc, System.Diagnostics.Trace.Listeners);
                        });
                    }
                    catch (Exception ex)
                    {
                        var message = $"Exception '{ex.GetType().Name}' occurred: {ex.Message}\r\nAdditional Information:\r\n{ex}";
                        sec.Exception(new InvalidDataException(message, ex));
                        System.Diagnostics.Trace.WriteLine(message);
                    }
                }
            }
        }
        #endregion

        public static CodeSection GetCodeSection(Type t, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var sec = new CodeSection(t, null, payload, TraceSource, sourceLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);
            return sec;
        }
        public static CodeSection GetCodeSection<T>(object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var sec = new CodeSection(typeof(T), null, payload, TraceSource, sourceLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);
            return sec;
        }
        public static CodeSection GetNamedSection(Type t, string name = null, object payload = null, SourceLevels sourceLevel = SourceLevels.Verbose, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var sec = new CodeSection(t, name, payload, TraceSource, sourceLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber);
            return sec;
        }

        public static void Trace(object obj, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Verbose, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Trace(obj, category, properties, source);
        }
#if NET6_0_OR_GREATER
        public static void Trace(ref TraceLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Verbose, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Trace(ref message, category, properties, source);
        }
        public static void Trace(string message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Verbose, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Trace(message, category, properties, source);
        }
#else
        public static void Trace(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Verbose, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Trace(message, category, properties, source);
        }
        public static void Trace(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Verbose, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Trace(message, category, properties, source);
        }
#endif
        public static void Trace(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Verbose, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Trace(getMessage, category, properties, source);
        }

        public static void Debug(object obj, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Verbose, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Debug(obj, category, properties, source);
        }
#if NET6_0_OR_GREATER
        public static void Debug(ref DebugLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Verbose, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Debug(ref message, category, properties, source);
        }
        public static void Debug(string message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Verbose, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Debug(message, category, properties, source);
        }
#else
        public static void Debug(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Verbose, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Debug(message, category, properties, source);
        }
        public static void Debug(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Verbose, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Debug(message, category, properties, source);
        }
#endif
        public static void Debug(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Verbose, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Debug(getMessage, category, properties, source);
        }
#if NET6_0_OR_GREATER
        public static void Information(ref InformationLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Information(ref message, category, properties, source);
        }
        public static void Information(string message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Information(message, category, properties, source);
        }
#else
        public static void Information(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Information(message, category, properties, source);
        }
        public static void Information(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Information(message, category, properties, source);
        }
#endif
        public static void Information(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Information, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Information(getMessage, category, properties, source);
        }
#if NET6_0_OR_GREATER
        public static void Warning(ref WarningLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Warning(ref message, category, properties, source);
        }
        public static void Warning(string message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Warning(message, category, properties, source);
        }
#else
        public static void Warning(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Warning(message, category, properties, source);
        }
        public static void Warning(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Warning(message, category, properties, source);
        }
#endif
        public static void Warning(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Warning, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Warning(getMessage, category, properties, source);
        }
#if NET6_0_OR_GREATER
        public static void Error(ref ErrorLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Error(ref message, category, properties, source);
        }
        public static void Error(string message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Error(message, category, properties, source);
        }
#else
        public static void Error(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Error(message, category, properties, source);
        }
        public static void Error(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Error(message, category, properties, source);
        }
#endif
        public static void Error(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Error(getMessage, category, properties, source);
        }
        public static void Exception(Exception exception, string category = null, IDictionary<string, object> properties = null, string source = null, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            var type = typeof(InternalClass);
            var caller = CodeSectionBase.Current.Value;
            var innerCodeSection = caller != null ? caller = caller.GetInnerSection() : caller = new CodeSection(type, null, null, null, SourceLevels.Error, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber, true);
            var innerCodeSectionLogger = innerCodeSection as ICodeSectionLogger;
            innerCodeSectionLogger.Exception(exception, category, properties, source);
        }

        // helpers
        #region GetModuleContext
        public static IModuleContext GetModuleContext(Assembly module)
        {
            if (!Modules.ContainsKey(module))
            {
                var moduleContext = new ModuleContext(module);
                Modules[module] = moduleContext;
            }
            return Modules[module];
        }
        #endregion
        #region GetMaxMessageLen
        public static int? GetMaxMessageLen(CodeSection section, TraceEventType traceEventType)
        {
            var maxMessageLenSpecific = default(int?);
            switch (traceEventType)
            {
                case TraceEventType.Error:
                case TraceEventType.Critical:
                    var maxMessageLenError = section?._maxMessageLenError ?? section?.ModuleContext?.MaxMessageLenError;
                    if (maxMessageLenError == null)
                    {
                        //var val = ConfigurationHelper.GetSetting("MaxMessageLenError", TraceManager.CONFIGDEFAULT_MAXMESSAGELENERROR);
                        var val = classConfigurationGetter.Get("MaxMessageLenError", TraceManager.CONFIGDEFAULT_MAXMESSAGELENERROR);
                        if (val != 0) { maxMessageLenError = val; if (section != null) { section._maxMessageLenError = maxMessageLenError; if (section.ModuleContext != null) { section.ModuleContext.MaxMessageLenError = maxMessageLenError; } } }
                    }
                    if (maxMessageLenError != 0) { maxMessageLenSpecific = maxMessageLenError; }
                    break;
                case TraceEventType.Warning:
                    var maxMessageLenWarning = section?._maxMessageLenWarning ?? section?.ModuleContext?.MaxMessageLenWarning;
                    if (maxMessageLenWarning == null)
                    {
                        //var val = ConfigurationHelper.GetSetting<int>("MaxMessageLenWarning", TraceManager.CONFIGDEFAULT_MAXMESSAGELENWARNING);
                        var val = classConfigurationGetter.Get("MaxMessageLenWarning", TraceManager.CONFIGDEFAULT_MAXMESSAGELENWARNING);
                        if (val != 0) { maxMessageLenWarning = val; if (section != null) { section._maxMessageLenWarning = maxMessageLenWarning; if (section.ModuleContext != null) { section.ModuleContext.MaxMessageLenWarning = maxMessageLenWarning; } } }
                    }
                    if (maxMessageLenWarning != 0) { maxMessageLenSpecific = maxMessageLenWarning; }
                    break;
                case TraceEventType.Information:
                    var maxMessageLenInfo = section?._maxMessageLenInfo ?? section?.ModuleContext?.MaxMessageLenInfo;
                    if (maxMessageLenInfo == null)
                    {
                        //var val = ConfigurationHelper.GetSetting<int>("MaxMessageLenInfo", TraceManager.CONFIGDEFAULT_MAXMESSAGELENINFO);
                        var val = classConfigurationGetter.Get("MaxMessageLenInfo", TraceManager.CONFIGDEFAULT_MAXMESSAGELENINFO);
                        if (val != 0) { maxMessageLenInfo = val; if (section != null) { section._maxMessageLenInfo = maxMessageLenInfo; if (section.ModuleContext != null) { section.ModuleContext.MaxMessageLenInfo = maxMessageLenInfo; } } }
                    }
                    if (maxMessageLenInfo != 0) { maxMessageLenSpecific = maxMessageLenInfo; }
                    break;
                case TraceEventType.Verbose:
                    var maxMessageLenVerbose = section?._maxMessageLenVerbose ?? section?.ModuleContext?.MaxMessageLenVerbose;
                    if (maxMessageLenVerbose == null)
                    {
                        //var val = ConfigurationHelper.GetSetting<int>("MaxMessageLenVerbose", TraceManager.CONFIGDEFAULT_MAXMESSAGELENINFO);
                        var val = classConfigurationGetter.Get("MaxMessageLenVerbose", TraceManager.CONFIGDEFAULT_MAXMESSAGELENINFO);
                        if (val != 0) { maxMessageLenVerbose = val; if (section != null) { section._maxMessageLenVerbose = maxMessageLenVerbose; if (section.ModuleContext != null) { section.ModuleContext.MaxMessageLenVerbose = maxMessageLenVerbose; } } }
                    }
                    if (maxMessageLenVerbose != 0) { maxMessageLenSpecific = maxMessageLenVerbose; }
                    break;
            }
            var maxMessageLen = maxMessageLenSpecific ?? section?._maxMessageLen ?? section?.ModuleContext?.MaxMessageLen;
            if (maxMessageLen == null)
            {
                //maxMessageLen = ConfigurationHelper.GetSetting<int>("MaxMessageLen", TraceManager.CONFIGDEFAULT_MAXMESSAGELEN);
                maxMessageLen = classConfigurationGetter.Get("MaxMessageLen", TraceManager.CONFIGDEFAULT_MAXMESSAGELEN);
                if (section != null) { section._maxMessageLen = maxMessageLen; if (section.ModuleContext != null) { section.ModuleContext.MaxMessageLen = maxMessageLen; } }
            }
            if (section != null) { section._maxMessageLen = maxMessageLen; }
            return maxMessageLen;
        }
        #endregion
        #region ApplyListenerConfig
        private static void ApplyListenerConfig(ListenerConfig listenerConfig, TraceListenerCollection listeners)
        {
            var action = listenerConfig.action ?? "add";
            var listenerType = listenerConfig.type;
            if (listenerConfig.innerListener != null)
            {
                for (var innerListener = listenerConfig.innerListener; innerListener != null; innerListener = innerListener.innerListener) { listenerType = $"{listenerType}/{innerListener.type}"; }
            }
            TraceManager.Information($"Listener action:'{action}', type:'{listenerType}'");

            var listener = default(TraceListener);
            listener = GetListenerFromConfig(listenerConfig, listeners);
            if (action.ToLower() != "remove" && listener != null)
            {
                listeners.Add(listener);
            }
        }
        #endregion
        #region GetListenerFromConfig
        private static TraceListener GetListenerFromConfig(ListenerConfig listenerConfig, TraceListenerCollection listeners, string defaultAction = "add")
        {
            var action = listenerConfig.action ?? defaultAction;
            switch (action.ToLower())
            {
                case "add":
                    {
                        var listener = default(TraceListener);
                        Type t = Type.GetType(listenerConfig.type);
                        try { listener = Activator.CreateInstance(t) as TraceListener; }
                        catch (Exception ex) { System.Diagnostics.Trace.WriteLine($"Failed to create Trace Listener '{listenerConfig.type}':\r\nAdditional information: {ex.Message}\r\n{ex.ToString()}"); }
                        if (listener != null)
                        {
                            listener.Name = listenerConfig.name;
                            if (listenerConfig.filter != null && !string.IsNullOrEmpty(listenerConfig.filter.initializeData))
                            {
                                if (listenerConfig.filter.type != null && (listenerConfig.filter.type.StartsWith("EventTypeFilter") || listenerConfig.filter.type.StartsWith("System.Diagnostics.EventTypeFilter")))
                                {
                                    var type = (SourceLevels)Enum.Parse(typeof(SourceLevels), listenerConfig.filter.initializeData);
                                    var filter = new EventTypeFilter(type);
                                    listener.Filter = filter;
                                }
                                if (listenerConfig.filter.type != null && (listenerConfig.filter.type.StartsWith("SourceFilter") || listenerConfig.filter.type.StartsWith("System.Diagnostics.SourceFilter")))
                                {
                                    var filter = !string.IsNullOrEmpty(listenerConfig.filter.initializeData) ? new SourceFilter(listenerConfig.filter.initializeData) : null;
                                    if (filter != null) { listener.Filter = filter; }
                                }
                            }
                            TraceListener innerListener = null;
                            var innerListenerConfig = listenerConfig.innerListener;
                            if (innerListenerConfig != null) { innerListener = GetListenerFromConfig(innerListenerConfig, listeners); }
                            if (innerListener != null)
                            {
                                var outerListener = listener as ISupportInnerListener;
                                outerListener.InnerListener = innerListener;
                            }
                        }
                        return listener;
                    }
                case "attach":
                    {
                        var listener = default(TraceListener);
                        Type t = Type.GetType(listenerConfig.type);
                        try { listener = Activator.CreateInstance(t) as TraceListener; }
                        catch (Exception ex) { System.Diagnostics.Trace.WriteLine($"Failed to create Trace Listener '{listenerConfig.type}':\r\nAdditional information: {ex.Message}\r\n{ex.ToString()}"); }
                        if (listener != null)
                        {
                            listener.Name = listenerConfig.name;
                            if (listenerConfig.filter != null && !string.IsNullOrEmpty(listenerConfig.filter.initializeData))
                            {
                                if (listenerConfig.filter.type != null && (listenerConfig.filter.type.StartsWith("EventTypeFilter") || listenerConfig.filter.type.StartsWith("System.Diagnostics.EventTypeFilter")))
                                {
                                    var type = (SourceLevels)Enum.Parse(typeof(SourceLevels), listenerConfig.filter.initializeData);
                                    var filter = new EventTypeFilter(type);
                                    listener.Filter = filter;
                                }
                                if (listenerConfig.filter.type != null && (listenerConfig.filter.type.StartsWith("SourceFilter") || listenerConfig.filter.type.StartsWith("System.Diagnostics.SourceFilter")))
                                {
                                    var filter = !string.IsNullOrEmpty(listenerConfig.filter.initializeData) ? new SourceFilter(listenerConfig.filter.initializeData) : null;
                                    if (filter != null) { listener.Filter = filter; }
                                }
                            }
                            TraceListener innerListener = null;
                            var innerListenerConfig = listenerConfig.innerListener;
                            if (innerListenerConfig != null) { innerListener = GetListenerFromConfig(innerListenerConfig, listeners); }
                            if (innerListener != null)
                            {
                                var outerListener = listener as ISupportInnerListener;
                                outerListener.InnerListener = innerListener;
                            }
                        }
                        return listener;
                    }
                case "remove":
                    {
                        var listener = default(TraceListener);
                        if (!string.IsNullOrEmpty(listenerConfig.name))
                        {
                            listener = listeners.OfType<TraceListener>().FirstOrDefault(l => l.Name == listenerConfig.name);
                            if (listener != null) { listeners.Remove(listener); }
                        }
                        else if (!string.IsNullOrEmpty(listenerConfig.type))
                        {
                            var t = Type.GetType(listenerConfig.type);
                            listener = listeners.OfType<TraceListener>().FirstOrDefault(l => t.IsAssignableFrom(l.GetType()));
                            if (listener != null) { listeners.Remove(listener); }
                        }
                        return listener;
                    }
                case "removeoradd":
                    {
                        var listener = default(TraceListener);
                        if (!string.IsNullOrEmpty(listenerConfig.name))
                        {
                            listener = listeners.OfType<TraceListener>().FirstOrDefault(l => l.Name == listenerConfig.name);
                            if (listener != null) { listeners.Remove(listener); }
                        }
                        else if (!string.IsNullOrEmpty(listenerConfig.type))
                        {
                            var t = Type.GetType(listenerConfig.type);
                            listener = listeners.OfType<TraceListener>().FirstOrDefault(l => t.IsAssignableFrom(l.GetType()));
                            if (listener != null) { listeners.Remove(listener); }
                        }
                        if (listener == null && !string.IsNullOrEmpty(listenerConfig.type))
                        {
                            Type t = Type.GetType(listenerConfig.type);
                            try { listener = Activator.CreateInstance(t) as TraceListener; }
                            catch (Exception ex) { System.Diagnostics.Trace.WriteLine($"Failed to create Trace Listener '{listenerConfig.type}':\r\nAdditional information: {ex.Message}\r\n{ex.ToString()}"); }
                            if (listener != null)
                            {
                                listener.Name = listenerConfig.name;
                                if (listenerConfig.filter != null && !string.IsNullOrEmpty(listenerConfig.filter.initializeData))
                                {
                                    if (listenerConfig.filter.type != null && (listenerConfig.filter.type.StartsWith("EventTypeFilter") || listenerConfig.filter.type.StartsWith("System.Diagnostics.EventTypeFilter")))
                                    {
                                        var type = (SourceLevels)Enum.Parse(typeof(SourceLevels), listenerConfig.filter.initializeData);
                                        var filter = new EventTypeFilter(type);
                                        listener.Filter = filter;
                                    }
                                    if (listenerConfig.filter.type != null && (listenerConfig.filter.type.StartsWith("SourceFilter") || listenerConfig.filter.type.StartsWith("System.Diagnostics.SourceFilter")))
                                    {
                                        var filter = !string.IsNullOrEmpty(listenerConfig.filter.initializeData) ? new SourceFilter(listenerConfig.filter.initializeData) : null;
                                        if (filter != null) { listener.Filter = filter; }
                                    }
                                }
                                TraceListener innerListener = null;
                                var innerListenerConfig = listenerConfig.innerListener;
                                if (innerListenerConfig != null) { innerListener = GetListenerFromConfig(innerListenerConfig, listeners); }
                                if (innerListener != null)
                                {
                                    var outerListener = listener as ISupportInnerListener;
                                    outerListener.InnerListener = innerListener;
                                }
                            }
                        }
                        return listener;
                    }
                case "clear":
                    {
                        listeners.Clear();
                    }
                    break;
            }

            return null;
        }
        #endregion

        public static IConfiguration GetConfiguration()
        {
            IConfiguration configuration = null;
            var jsonFileName = "appsettings";
            var currentDirectory = Directory.GetCurrentDirectory();
            var appdomainFolder = System.AppDomain.CurrentDomain.BaseDirectory.Trim('\\');
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            if (assembly == null) { assembly = System.Reflection.Assembly.GetExecutingAssembly(); }

            var environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLower();
            if (string.IsNullOrEmpty(environment)) { environment = System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")?.ToLower(); }
            if (string.IsNullOrEmpty(environment)) { environment = System.Environment.GetEnvironmentVariable("ENVIRONMENT")?.ToLower(); }
            if (string.IsNullOrEmpty(environment)) { environment = "production"; }

            var jsonFile = currentDirectory == appdomainFolder ? $"{jsonFileName}.json" : Path.Combine(appdomainFolder, $"{jsonFileName}.json");
            var builder = default(IConfigurationBuilder);
            DebugHelper.IfDebug(() =>
            {   // for debug build only check environment setting on appsettings.json
                builder = new ConfigurationBuilder()
                              .AddJsonFile(jsonFile, true, true);

                try { builder = builder.AddUserSecrets(assembly); } catch (Exception ex) { /* ignore user secrets if not accessible */ }

                builder = builder.AddInMemoryCollection();

                builder.AddEnvironmentVariables();
                configuration = builder.Build();
                var jsonEnvironment = configuration.GetValue($"AppSettings:Environment", "");
                if (string.IsNullOrEmpty(jsonEnvironment)) { environment = jsonEnvironment; }
            });

            var environmentJsonFile = currentDirectory == appdomainFolder ? $"{jsonFileName}.json" : Path.Combine(appdomainFolder, $"{jsonFileName}.{environment}.json");
            builder = new ConfigurationBuilder()
                      .AddJsonFile(jsonFile, true, true);
            if (File.Exists(environmentJsonFile)) { builder = builder.AddJsonFile(environmentJsonFile, true, true); }
            builder = builder.AddInMemoryCollection();
            builder.AddEnvironmentVariables();
            configuration = builder.Build();

            TraceManager.EnvironmentName = environment;

            return configuration;
        }
    }
}

