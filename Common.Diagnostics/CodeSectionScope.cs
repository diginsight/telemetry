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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NanoId = Nanoid.Nanoid;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualBasic;
//using static System.Formats.Asn1.AsnWriter;
#endregion

namespace Common
{
    public class CodeSectionScope : CodeSectionBase, ICodeSection, IDisposable, ICloneable, ICodeSectionLogger
    {
        public ILogger logger = null;
        public ActivitySource activitySource = null;
        public static readonly Type classConfigurationGetterGenericType = typeof(IClassConfigurationGetter<>);

        #region .ctor
        static CodeSectionScope() { }
        public CodeSectionScope(CodeSectionScope pCopy)
        {
            this.Name = pCopy.Name;
            this.Payload = pCopy.Payload;
            this.TraceSource = pCopy.TraceSource;
            this.SourceLevel = pCopy.SourceLevel;
            this.LogLevel = pCopy.LogLevel;
            this.MemberName = pCopy.MemberName;
            this.SourceFilePath = pCopy.SourceFilePath;
            this.SourceLineNumber = pCopy.SourceLineNumber;
            this.DisableStartEndTraces = true;

            logger = pCopy.logger;
            //this.TraceLoggerMinimumLevelService = pCopy.TraceLoggerMinimumLevelService;
            this.ClassConfigurationGetter = pCopy.ClassConfigurationGetter;
            this.MinimumLogLevel = pCopy.MinimumLogLevel;
            this.PublishFlow = pCopy.PublishFlow;
            this.PublishMetrics = pCopy.PublishMetrics;
            this._isLogEnabled = pCopy._isLogEnabled;

            this.T = pCopy.T;
            this.Assembly = pCopy.Assembly;
            this.Category = pCopy.Category;
            this.Source = pCopy.Source;
            this.CallStartMilliseconds = pCopy.CallStartMilliseconds;

            Caller = CodeSectionBase.Current.Value;

            this.NestingLevel = pCopy.NestingLevel;
            this.OperationID = pCopy.OperationID;
            this.OperationDept = pCopy.OperationDept;

            this.IsDisposed = pCopy.IsDisposed;
            this.Caller = pCopy.Caller;
            this.ModuleContext = pCopy.ModuleContext;
        }

        public CodeSectionScope(ActivitySource activitySource, ILogger logger, Type type, string name = null, object payload = null,
                                TraceSource traceSource = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel logLevel = LogLevel.Debug,
                                string category = null, IDictionary<string, object> properties = null, string source = null, long startTicks = 0,
                                [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0,
                                bool disableStartEndTraces = false)
        {
            this.Name = name;
            this.Payload = payload;
            this.TraceSource = traceSource;
            this.SourceLevel = sourceLevel;
            this.LogLevel = logLevel;
            this.MemberName = memberName;
            this.SourceFilePath = sourceFilePath;
            this.SourceLineNumber = sourceLineNumber;
            this.DisableStartEndTraces = disableStartEndTraces;

            this.logger = logger;

            var services = TraceLogger.Services;

            IClassConfigurationGetter classConfigurationGetter = default;
            if (services != null)
            {
                var classConfigurationGetterType = classConfigurationGetterGenericType.MakeGenericType(type ?? typeof(CodeSectionScope));
                try { classConfigurationGetter = services.GetService(classConfigurationGetterType) as IClassConfigurationGetter; } catch (Exception _) { }
                this.ClassConfigurationGetter = classConfigurationGetter;
            }
            this.MinimumLogLevel = classConfigurationGetter?.Get("TraceLoggerMinimumLevel", LogLevel.Trace) ?? LogLevel.Trace;
            this.PublishFlow = classConfigurationGetter?.Get("PublishFlow", false) ?? false;
            this.PublishMetrics = classConfigurationGetter?.Get("PublishMetrics", false) ?? false;
            //this._isLogEnabled = logLevel >= this.MinimumLogLevel;

            if (type == null && logger != null) { type = logger.GetType().GenericTypeArguments.FirstOrDefaultChecked(); }

            this.T = type;
            this.Assembly = type?.Assembly;
            this.Category = category;
            if (string.IsNullOrEmpty(source)) { source = this.Assembly?.GetName()?.Name; }

            this.Properties = properties;
            this.Source = source;
            this.CallStartMilliseconds = _stopwatch.ElapsedMilliseconds;
            this.CallStartTicks = startTicks;

            var caller = CodeSectionBase.Current.Value;
            while (caller != null && caller.IsDisposed) { caller = caller.Caller; } // _disposed
            Caller = caller;

            if (disableStartEndTraces == false) { CodeSectionBase.Current.Value = this; } // disableStartEndTraces

            if (Caller != null)
            {
                if (disableStartEndTraces == false) { NestingLevel = Caller.NestingLevel + 1; } // NestingLevel
                var operationId = Caller.OperationID;
                if (_overrideOperationIdEnabled)
                {
                    var operationIdOverride = properties != null && properties.ContainsKey("OperationId") ? properties["OperationId"] as string : null;
                    if (operationIdOverride != null) { operationId = operationIdOverride; }
                }
                OperationID = operationId;
                OperationDept = Caller.OperationDept; // OperationDept
                if (string.IsNullOrEmpty(OperationID)) { (this.OperationID, this.OperationDept) = getOperationInfo(); }
            }
            else
            {
                (string operationId, int operationDept) = getOperationInfo();
                NestingLevel = 0;
                if (_overrideOperationIdEnabled)
                {
                    var operationIdOverride = properties != null && properties.ContainsKey("OperationId") ? properties["OperationId"] as string : null;
                    if (operationIdOverride != null) { operationId = operationIdOverride; }
                }
                OperationID = operationId;
                OperationDept = operationDept;
            }

            if (this.DisableStartEndTraces == true) { return; }

            if (this.PublishFlow || this.PublishMetrics)
            {
                var fullCallerMemberName = !string.IsNullOrEmpty(this.Name) ? $"{this.MemberName}.{this.Name}" : this.MemberName;
                if (activitySource == null) { activitySource = TraceLogger.ActivitySource; }
                var activity = activitySource.StartActivity($"{type?.Name ?? this.ClassName}.{fullCallerMemberName}"); // , ActivityKind.Internal
                //activity.SetCustomProperty("Scope", scope);
                //activity.SetCustomProperty("Logger", logger);
                //activity.SetCustomProperty("LogLevel", logLevel);
                //activity.SetCustomProperty("Payload", payload);
                if (this.Properties == null) { this.Properties = new Dictionary<string, object>(); }
                this.Properties.Add("Activity", activity);
            }

            // if (this?._isLogEnabled == false) { return; }
            if (logLevel < this.MinimumLogLevel) { return; }

            var entry = new TraceEntry() { TraceEventType = TraceEventType.Start, LogLevel = logLevel, TraceSource = this.TraceSource, Message = null, Properties = properties, Source = source, Category = category, SourceLevel = sourceLevel, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (this.logger == null) { this.logger = GetEntrylogger(ref entry); }

                this.logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }
        }

        internal CodeSectionScope(ActivitySource activitySource, ILogger logger, string typeName, string name = null, object payload = null,
                                  TraceSource traceSource = null, SourceLevels sourceLevel = SourceLevels.Verbose, LogLevel logLevel = LogLevel.Debug,
                                  string category = null, IDictionary<string, object> properties = null, string source = null, long startTicks = 0,
                                  [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0,
                                  bool disableStartEndTraces = false)
        {
            this.Name = name;
            this.Payload = payload;
            this.TraceSource = traceSource;
            this.SourceLevel = sourceLevel;
            this.LogLevel = logLevel;
            this.MemberName = memberName;
            this.SourceFilePath = sourceFilePath;
            this.SourceLineNumber = sourceLineNumber;
            this.DisableStartEndTraces = disableStartEndTraces;

            this.logger = logger;

            var type = logger?.GetType()?.GenericTypeArguments?.FirstOrDefault();
            this.T = type;
            this.TypeName = typeName;

            var services = TraceLogger.Services;

            IClassConfigurationGetter classConfigurationGetter = null;
            if (services != null)
            {
                var classConfigurationGetterType = classConfigurationGetterGenericType.MakeGenericType(type ?? typeof(CodeSectionScope));
                try { classConfigurationGetter = services?.GetService(classConfigurationGetterType) as IClassConfigurationGetter; } catch (Exception _) { }
                this.ClassConfigurationGetter = classConfigurationGetter;
            }
            //this.ScopedConfiguration = scopedConfiguration;
            this.MinimumLogLevel = classConfigurationGetter?.Get("MinimumLevel", LogLevel.Trace) ?? LogLevel.Trace;
            this.PublishFlow = classConfigurationGetter?.Get("PublishFlow", false) ?? false;
            this.PublishMetrics = classConfigurationGetter?.Get("PublishMetrics", false) ?? false;

            if (!string.IsNullOrEmpty(typeName))
            {
                var classNameIndex = typeName.LastIndexOf('.') + 1;
                var className = classNameIndex >= 0 ? typeName.Substring(classNameIndex) : typeName;
                this.ClassName = className;
            }
            else { this.ClassName = "Unknown"; }

            this.Assembly = type?.Assembly;
            this.Category = category;
            if (string.IsNullOrEmpty(source)) { source = this.Assembly?.GetName()?.Name; }

            this.Properties = properties;
            this.Source = source;
            this.CallStartMilliseconds = _stopwatch.ElapsedMilliseconds;
            this.CallStartTicks = startTicks;

            var caller = CodeSectionBase.Current.Value;
            while (caller != null && caller.IsDisposed) { caller = caller.Caller; }
            Caller = caller;

            if (disableStartEndTraces == false) { CodeSectionBase.Current.Value = this; }

            if (Caller != null)
            {
                if (disableStartEndTraces == false) { NestingLevel = Caller.NestingLevel + 1; }
                var operationId = Caller.OperationID;
                if (_overrideOperationIdEnabled)
                {
                    var operationIdOverride = properties != null && properties.ContainsKey("OperationId") ? properties["OperationId"] as string : null;
                    if (operationIdOverride != null) { operationId = operationIdOverride; }
                }
                OperationID = operationId;
                OperationDept = Caller.OperationDept;
                if (string.IsNullOrEmpty(OperationID)) { (this.OperationID, this.OperationDept) = getOperationInfo(); }
            }
            else
            {
                (string operationId, int operationDept) = getOperationInfo();
                NestingLevel = 0;
                if (_overrideOperationIdEnabled)
                {
                    var operationIdOverride = properties != null && properties.ContainsKey("OperationId") ? properties["OperationId"] as string : null;
                    if (operationIdOverride != null) { operationId = operationIdOverride; }
                }
                OperationID = operationId; // overrideOperationId && opId!=null? opId: operationID;
                OperationDept = operationDept;
            }
            //this.ModuleContext = this.Assembly != null ? LoggerFormatter.GetModuleContext(this.Assembly) : null;

            if (this.DisableStartEndTraces == true) { return; }

            if (this.PublishFlow || this.PublishMetrics)
            {
                var fullCallerMemberName = !string.IsNullOrEmpty(this.Name) ? $"{this.MemberName}.{this.Name}" : this.MemberName;
                if (activitySource == null) { activitySource = TraceLogger.ActivitySource; }
                var activity = activitySource.StartActivity($"{type?.Name ?? this.ClassName}.{fullCallerMemberName}"); // , ActivityKind.Internal
                //activity.SetCustomProperty("Scope", scope);
                //activity.SetCustomProperty("Logger", logger);
                //activity.SetCustomProperty("LogLevel", logLevel);
                //activity.SetCustomProperty("Payload", payload);
                if (this.Properties == null) { this.Properties = new Dictionary<string, object>(); }
                this.Properties.Add("Activity", activity);
            }

            //if (this?._isLogEnabled == false) { return; }
            if (logLevel < this.MinimumLogLevel) { return; }

            var entry = new TraceEntry() { TraceEventType = TraceEventType.Start, TraceSource = this.TraceSource, SourceLevel = sourceLevel, LogLevel = logLevel, Message = null, Properties = properties, Source = source, Category = category, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (this.logger == null) { this.logger = GetEntrylogger(ref entry); }

                this.logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }
        }
        #endregion

        public void LogTrace(object obj, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            //if (this?._isLogEnabled == false) { return; }
            var logLevel = LogLevel.Trace;
            if (logLevel < this.MinimumLogLevel) { return; }

            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var entry = new TraceEntry() { TraceEventType = TraceEventType.Verbose, SourceLevel = SourceLevels.Verbose, LogLevel = LogLevel.Trace, Properties = properties, Source = source ?? this.Source, Category = category, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };

            if (obj is Func<string>) { entry.GetMessage = (Func<string>)obj; }
            else if (obj is Func<object>) { entry.GetMessage = () => ((Func<object>)obj)().GetLogString(); }
            else if (obj is string) { entry.Message = (string)obj; }
            else { entry.MessageObject = obj; }

            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (logger == null) { logger = GetEntrylogger(ref entry); }

                logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }
        }
#if NET6_0_OR_GREATER
        public void LogTrace([InterpolatedStringHandlerArgument("")] ref TraceLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            //if (this?._isLogEnabled == false) { return; }
            var logLevel = LogLevel.Trace;
            if (logLevel < this.MinimumLogLevel) { return; }

            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var entry = new TraceEntry()
            {
                //MessageFormat = message.FormatTemplate?.ToString(),
                //MessageArgs = message.FormatParameters,
                GetMessage = new Func<string>(message.FinalString.ToString),
                TraceEventType = TraceEventType.Verbose,
                SourceLevel = SourceLevels.Verbose,
                LogLevel = LogLevel.Trace,
                Properties = properties,
                Source = source ?? this.Source,
                Category = category,
                CodeSectionBase = this,
                Thread = Thread.CurrentThread,
                ThreadID = Thread.CurrentThread.ManagedThreadId,
                ApartmentState = Thread.CurrentThread.GetApartmentState(),
                DisableCRLFReplace = disableCRLFReplace,
                ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds,
                TraceStartTicks = startTicks
            };
            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (logger == null) { logger = GetEntrylogger(ref entry); }

                logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }
        }
        public void LogTrace(string message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            //if (this?._isLogEnabled == false) { return; }
            var logLevel = LogLevel.Trace;
            if (logLevel < this.MinimumLogLevel) { return; }

            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var entry = new TraceEntry() { Message = message, TraceEventType = TraceEventType.Verbose, SourceLevel = SourceLevels.Verbose, LogLevel = LogLevel.Trace, Properties = properties, Source = source ?? this.Source, Category = category, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (logger == null) { logger = GetEntrylogger(ref entry); }

                logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }
        }
#else
        public void LogTrace(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var entry = new TraceEntry() { Message = message.Value, TraceEventType = TraceEventType.Verbose, SourceLevel = SourceLevels.Verbose, LogLevel = LogLevel.Trace, Properties = properties, Source = source ?? this.Source, Category = category, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (logger == null) { logger = GetEntrylogger(ref entry); }

                logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }
        }
        public void LogTrace(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            try
            {
                var entry = new TraceEntry()
                {
                    Message = string.Format(message.Format, message.GetArguments()),
                    TraceEventType = TraceEventType.Verbose,
                    SourceLevel = SourceLevels.Verbose,
                    LogLevel = LogLevel.Trace,
                    Properties = properties,
                    Source = source ?? this.Source,
                    Category = category,
                    CodeSectionBase = this,
                    Thread = Thread.CurrentThread,
                    ThreadID = Thread.CurrentThread.ManagedThreadId,
                    ApartmentState = Thread.CurrentThread.GetApartmentState(),
                    DisableCRLFReplace = disableCRLFReplace,
                    ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds,
                    TraceStartTicks = startTicks
                };
                if (!TraceLogger._lockListenersNotifications.Value)
                {
                    if (logger == null) { logger = GetEntrylogger(ref entry); }

                    logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                    {
                        if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                        var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                        return ret;
                    });
                }
                else
                {
                    TraceLogger._pendingEntries.Enqueue(entry);
                    //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
                }
            }
            catch (Exception) { }
        }
#endif
        public void LogTrace(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            //if (this?._isLogEnabled == false) { return; }
            var logLevel = LogLevel.Trace;
            if (logLevel < this.MinimumLogLevel) { return; }

            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            try
            {
                var entry = new TraceEntry() { GetMessage = getMessage, TraceEventType = TraceEventType.Verbose, SourceLevel = SourceLevels.Verbose, LogLevel = LogLevel.Trace, Properties = properties, Source = source ?? this.Source, Category = category, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
                if (!TraceLogger._lockListenersNotifications.Value)
                {
                    if (logger == null) { logger = GetEntrylogger(ref entry); }

                    logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                    {
                        if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                        var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                        return ret;
                    });
                }
                else
                {
                    TraceLogger._pendingEntries.Enqueue(entry);
                    //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
                }
            }
            catch (Exception) { }
        }

        public void LogDebug(object obj, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            //if (this?._isLogEnabled == false) { return; }
            var logLevel = LogLevel.Debug;
            if (logLevel < this.MinimumLogLevel) { return; }

            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var entry = new TraceEntry() { TraceEventType = TraceEventType.Verbose, SourceLevel = SourceLevels.Verbose, LogLevel = LogLevel.Debug, Properties = properties, Source = source ?? this.Source, Category = category, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };

            if (obj is Func<string>) { entry.GetMessage = (Func<string>)obj; }
            else if (obj is Func<object>) { entry.GetMessage = () => ((Func<object>)obj)().GetLogString(); }
            else if (obj is string) { entry.Message = (string)obj; }
            else { entry.MessageObject = obj; }

            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (logger == null) { logger = GetEntrylogger(ref entry); }

                logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }
        }
#if NET6_0_OR_GREATER
        public void LogDebug([InterpolatedStringHandlerArgument("")] ref DebugLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            //if (this?._isLogEnabled == false) { return; }
            var logLevel = LogLevel.Debug;
            if (logLevel < this.MinimumLogLevel) { return; }

            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var entry = new TraceEntry()
            {
                //MessageFormat = message.FormatTemplate?.ToString(),
                //MessageArgs = message.FormatParameters,
                GetMessage = new Func<string>(message.FinalString.ToString),
                TraceEventType = TraceEventType.Verbose,
                SourceLevel = SourceLevels.Verbose,
                LogLevel = LogLevel.Debug,
                Properties = properties,
                Source = source ?? this.Source,
                Category = category,
                CodeSectionBase = this,
                Thread = Thread.CurrentThread,
                ThreadID = Thread.CurrentThread.ManagedThreadId,
                ApartmentState = Thread.CurrentThread.GetApartmentState(),
                DisableCRLFReplace = disableCRLFReplace,
                ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds,
                TraceStartTicks = startTicks
            };
            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (logger == null) { logger = GetEntrylogger(ref entry); }

                logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }
        }
        public void LogDebug(string message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            //if (this?._isLogEnabled == false) { return; }
            var logLevel = LogLevel.Debug;
            if (logLevel < this.MinimumLogLevel) { return; }

            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var entry = new TraceEntry() { Message = message, TraceEventType = TraceEventType.Verbose, SourceLevel = SourceLevels.Verbose, LogLevel = LogLevel.Debug, Properties = properties, Source = source ?? this.Source, Category = category, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (logger == null) { logger = GetEntrylogger(ref entry); }

                logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }
        }
#else
        public void LogDebug(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var entry = new TraceEntry() { Message = message.Value, TraceEventType = TraceEventType.Verbose, SourceLevel = SourceLevels.Verbose, LogLevel = LogLevel.Debug, Properties = properties, Source = source ?? this.Source, Category = category, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (logger == null) { logger = GetEntrylogger(ref entry); }

                logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }
        }
        public void LogDebug(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            try
            {
                var entry = new TraceEntry()
                {
                    Message = string.Format(message.Format, message.GetArguments()),
                    TraceEventType = TraceEventType.Verbose,
                    SourceLevel = SourceLevels.Verbose,
                    LogLevel = LogLevel.Debug,
                    Properties = properties,
                    Source = source ?? this.Source,
                    Category = category,
                    CodeSectionBase = this,
                    Thread = Thread.CurrentThread,
                    ThreadID = Thread.CurrentThread.ManagedThreadId,
                    ApartmentState = Thread.CurrentThread.GetApartmentState(),
                    DisableCRLFReplace = disableCRLFReplace,
                    ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds,
                    TraceStartTicks = startTicks
                };
                if (!TraceLogger._lockListenersNotifications.Value)
                {
                    if (logger == null) { logger = GetEntrylogger(ref entry); }

                    logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
                }
                else
                {
                    TraceLogger._pendingEntries.Enqueue(entry);
                    //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
                }
            }
            catch (Exception) { }
        }
#endif
        public void LogDebug(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            //if (this?._isLogEnabled == false) { return; }
            var logLevel = LogLevel.Debug;
            if (logLevel < this.MinimumLogLevel) { return; }

            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            try
            {
                var entry = new TraceEntry() { GetMessage = getMessage, TraceEventType = TraceEventType.Verbose, SourceLevel = SourceLevels.Verbose, LogLevel = LogLevel.Debug, Properties = properties, Source = source ?? this.Source, Category = category, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
                if (!TraceLogger._lockListenersNotifications.Value)
                {
                    if (logger == null) { logger = GetEntrylogger(ref entry); }

                    logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                    {
                        if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                        var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                        return ret;
                    });
                }
                else
                {
                    TraceLogger._pendingEntries.Enqueue(entry);
                    //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
                }
            }
            catch (Exception) { }
        }

#if NET6_0_OR_GREATER
        public void LogInformation([InterpolatedStringHandlerArgument("")] ref InformationLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            //if (this?._isLogEnabled == false) { return; }
            var logLevel = LogLevel.Information;
            if (logLevel < this.MinimumLogLevel) { return; }

            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var entry = new TraceEntry()
            {
                //MessageFormat = message.FormatTemplate?.ToString(),
                //MessageArgs = message.FormatParameters,
                GetMessage = new Func<string>(message.FinalString.ToString),
                TraceEventType = TraceEventType.Information,
                SourceLevel = SourceLevels.Information,
                LogLevel = LogLevel.Information,
                TraceSource = this.TraceSource,
                Properties = properties,
                Source = source ?? this.Source,
                Category = category,
                CodeSectionBase = this,
                Thread = Thread.CurrentThread,
                ThreadID = Thread.CurrentThread.ManagedThreadId,
                ApartmentState = Thread.CurrentThread.GetApartmentState(),
                DisableCRLFReplace = disableCRLFReplace,
                ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds,
                TraceStartTicks = startTicks
            };
            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (logger == null) { logger = GetEntrylogger(ref entry); }

                logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }
        }
        public void LogInformation(string message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            //if (this?._isLogEnabled == false) { return; }
            var logLevel = LogLevel.Information;
            if (logLevel < this.MinimumLogLevel) { return; }

            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var entry = new TraceEntry() { Message = message, TraceEventType = TraceEventType.Information, SourceLevel = SourceLevels.Information, LogLevel = LogLevel.Information, TraceSource = this.TraceSource, Properties = properties, Source = source ?? this.Source, Category = category, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (logger == null) { logger = GetEntrylogger(ref entry); }

                logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }
        }
#else
        public void LogInformation(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var entry = new TraceEntry() { Message = message.Value, TraceEventType = TraceEventType.Information, SourceLevel = SourceLevels.Information, LogLevel = LogLevel.Information, TraceSource = this.TraceSource, Properties = properties, Source = source ?? this.Source, Category = category, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (logger == null) { logger = GetEntrylogger(ref entry); }

                logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }
        }
        public void LogInformation(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var entry = new TraceEntry() { Message = string.Format(message.Format, message.GetArguments()), TraceEventType = TraceEventType.Information, SourceLevel = SourceLevels.Information, LogLevel = LogLevel.Information, TraceSource = this.TraceSource, Properties = properties, Source = source ?? this.Source, Category = category, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (logger == null) { logger = GetEntrylogger(ref entry); }

                logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }
        }
#endif
        public void LogInformation(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            //if (this?._isLogEnabled == false) { return; }
            var logLevel = LogLevel.Information;
            if (logLevel < this.MinimumLogLevel) { return; }

            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            try
            {
                var entry = new TraceEntry() { GetMessage = getMessage, TraceEventType = TraceEventType.Information, SourceLevel = SourceLevels.Information, LogLevel = LogLevel.Information, Properties = properties, Source = source ?? this.Source, Category = category, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
                if (!TraceLogger._lockListenersNotifications.Value)
                {
                    if (logger == null) { logger = GetEntrylogger(ref entry); }

                    logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                    {
                        if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                        var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                        return ret;
                    });
                }
                else
                {
                    TraceLogger._pendingEntries.Enqueue(entry);
                    //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
                }
            }
            catch (Exception) { }
        }

#if NET6_0_OR_GREATER
        public void LogWarning([InterpolatedStringHandlerArgument("")] ref WarningLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            //if (this?._isLogEnabled == false) { return; }
            var logLevel = LogLevel.Warning;
            if (logLevel < this.MinimumLogLevel) { return; }

            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var entry = new TraceEntry()
            {
                //MessageFormat = message.FormatTemplate?.ToString(),
                //MessageArgs = message.FormatParameters,
                GetMessage = new Func<string>(message.FinalString.ToString),
                TraceEventType = TraceEventType.Warning,
                SourceLevel = SourceLevels.Warning,
                LogLevel = LogLevel.Warning,
                TraceSource = this.TraceSource,
                Properties = properties,
                Source = source ?? this.Source,
                Category = category,
                CodeSectionBase = this,
                Thread = Thread.CurrentThread,
                ThreadID = Thread.CurrentThread.ManagedThreadId,
                ApartmentState = Thread.CurrentThread.GetApartmentState(),
                DisableCRLFReplace = disableCRLFReplace,
                ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds,
                TraceStartTicks = startTicks
            };
            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (logger == null) { logger = GetEntrylogger(ref entry); }

                logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }

        }
        public void LogWarning(string message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            //if (this?._isLogEnabled == false) { return; }
            var logLevel = LogLevel.Warning;
            if (logLevel < this.MinimumLogLevel) { return; }

            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var entry = new TraceEntry() { Message = message, TraceEventType = TraceEventType.Warning, SourceLevel = SourceLevels.Warning, LogLevel = LogLevel.Warning, TraceSource = this.TraceSource, Properties = properties, Source = source ?? this.Source, Category = category, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (logger == null) { logger = GetEntrylogger(ref entry); }

                logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }

        }
#else
        public void LogWarning(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var entry = new TraceEntry() { Message = message.Value, TraceEventType = TraceEventType.Warning, SourceLevel = SourceLevels.Warning, LogLevel = LogLevel.Warning, TraceSource = this.TraceSource, Properties = properties, Source = source ?? this.Source, Category = category, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (logger == null) { logger = GetEntrylogger(ref entry); }

                logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }

        }
        public void LogWarning(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var entry = new TraceEntry() { Message = string.Format(message.Format, message.GetArguments()), TraceEventType = TraceEventType.Warning, SourceLevel = SourceLevels.Warning, LogLevel = LogLevel.Warning, TraceSource = this.TraceSource, Properties = properties, Source = source ?? this.Source, Category = category, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (logger == null) { logger = GetEntrylogger(ref entry); }

                logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }
        }
#endif
        public void LogWarning(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            //if (this?._isLogEnabled == false) { return; }
            var logLevel = LogLevel.Warning;
            if (logLevel < this.MinimumLogLevel) { return; }

            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            try
            {
                var entry = new TraceEntry() { GetMessage = getMessage, TraceEventType = TraceEventType.Warning, SourceLevel = SourceLevels.Warning, LogLevel = LogLevel.Warning, Properties = properties, Source = source ?? this.Source, Category = category, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
                if (!TraceLogger._lockListenersNotifications.Value)
                {
                    if (logger == null) { logger = GetEntrylogger(ref entry); }

                    logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                    {
                        if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                        var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                        return ret;
                    });
                }
                else
                {
                    TraceLogger._pendingEntries.Enqueue(entry);
                    //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
                }
            }
            catch (Exception) { }
        }

#if NET6_0_OR_GREATER
        public void LogError([InterpolatedStringHandlerArgument("")] ref ErrorLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            //if (this?._isLogEnabled == false) { return; }
            var logLevel = LogLevel.Error;
            if (logLevel < this.MinimumLogLevel) { return; }

            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var entry = new TraceEntry()
            {
                //MessageFormat = message.FormatTemplate?.ToString(),
                //MessageArgs = message.FormatParameters,
                GetMessage = new Func<string>(message.FinalString.ToString),
                TraceEventType = TraceEventType.Error,
                SourceLevel = SourceLevels.Error,
                LogLevel = LogLevel.Error,
                TraceSource = this.TraceSource,
                Properties = properties,
                Source = source ?? this.Source,
                Category = category,
                CodeSectionBase = this,
                Thread = Thread.CurrentThread,
                ThreadID = Thread.CurrentThread.ManagedThreadId,
                ApartmentState = Thread.CurrentThread.GetApartmentState(),
                DisableCRLFReplace = disableCRLFReplace,
                ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds,
                TraceStartTicks = startTicks
            };
            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (logger == null) { logger = GetEntrylogger(ref entry); }

                logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }
        }
        public void LogError(string message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            //if (this?._isLogEnabled == false) { return; }
            var logLevel = LogLevel.Error;
            if (logLevel < this.MinimumLogLevel) { return; }

            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var entry = new TraceEntry() { Message = message, TraceEventType = TraceEventType.Error, SourceLevel = SourceLevels.Error, LogLevel = LogLevel.Error, TraceSource = this.TraceSource, Properties = properties, Source = source ?? this.Source, Category = category, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (logger == null) { logger = GetEntrylogger(ref entry); }

                logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }
        }
#else
        public void LogError(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var entry = new TraceEntry() { Message = message.Value, TraceEventType = TraceEventType.Error, SourceLevel = SourceLevels.Error, LogLevel = LogLevel.Error, TraceSource = this.TraceSource, Properties = properties, Source = source ?? this.Source, Category = category, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (logger == null) { logger = GetEntrylogger(ref entry); }

                logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }
        }
        public void LogError(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            var entry = new TraceEntry() { Message = string.Format(message.Format, message.GetArguments()), TraceEventType = TraceEventType.Error, SourceLevel = SourceLevels.Error, LogLevel = LogLevel.Error, TraceSource = this.TraceSource, Properties = properties, Source = source ?? this.Source, Category = category, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (logger == null) { logger = GetEntrylogger(ref entry); }

                logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }
        }
#endif
        public void LogError(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            //if (this?._isLogEnabled == false) { return; }
            var logLevel = LogLevel.Error;
            if (logLevel < this.MinimumLogLevel) { return; }

            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;

            try
            {
                var entry = new TraceEntry() { GetMessage = getMessage, TraceEventType = TraceEventType.Error, SourceLevel = SourceLevels.Error, LogLevel = LogLevel.Error, Properties = properties, Source = source ?? this.Source, Category = category, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
                if (!TraceLogger._lockListenersNotifications.Value)
                {
                    if (logger == null) { logger = GetEntrylogger(ref entry); }

                    logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                    {
                        if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                        var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                        return ret;
                    });
                }
                else
                {
                    TraceLogger._pendingEntries.Enqueue(entry);
                    //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
                }
            }
            catch (Exception) { }
        }

        public void LogException(Exception exception, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = true)
        {
            //if (this?._isLogEnabled == false) { return; }
            var logLevel = LogLevel.Error;
            if (logLevel < this.MinimumLogLevel) { return; }

            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            if (exception == null) return;

            var entry = new TraceEntry()
            {
                TraceEventType = TraceEventType.Critical,
                SourceLevel = SourceLevels.Critical,
                TraceSource = this.TraceSource,
                LogLevel = LogLevel.Error,
                Message = $"Exception: {exception.Message} (InnerException: {exception?.InnerException?.Message ?? "null"})\nStackTrace: {exception.StackTrace}",
                Properties = properties,
                Source = source ?? this.Source,
                Category = category,
                CodeSectionBase = this,
                Thread = Thread.CurrentThread,
                ThreadID = Thread.CurrentThread.ManagedThreadId,
                ApartmentState = Thread.CurrentThread.GetApartmentState(),
                DisableCRLFReplace = disableCRLFReplace,
                ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds,
                TraceStartTicks = startTicks,
                Exception = exception
            };
            if (!TraceLogger._lockListenersNotifications.Value)
            {
                if (logger == null) { logger = GetEntrylogger(ref entry); }

                logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                {
                    if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                    var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                    return ret;
                });
            }
            else
            {
                TraceLogger._pendingEntries.Enqueue(entry);
                //if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.Init(null); }
            }
        }

        public override void Dispose()
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            if (IsDisposed) { return; }

            var activityObject = default(object);
            var activity = default(Activity);
            var ok = this.Properties?.TryGetValue("Activity", out activityObject) ?? false;
            if (ok)
            {
                activity = activityObject as Activity;
                if (activity != null)
                {
                    activity.Dispose();
                    this.Properties.Remove("Activity");
                }
            }

            base.Dispose();

            try
            {
                if (this.DisableStartEndTraces == true) { return; }
                //if (this?._isLogEnabled == false) { return; }
                if (this.LogLevel < this.MinimumLogLevel) { return; }

                if (!TraceLogger._lockListenersNotifications.Value)
                {
                    var entry = new TraceEntry() { TraceEventType = TraceEventType.Stop, LogLevel = this.LogLevel, TraceSource = this.TraceSource, Message = null, Properties = this.Properties, Source = this.Source, Category = this.Category, SourceLevel = this.SourceLevel, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
                    if (logger == null) { logger = GetEntrylogger(ref entry); }

                    logger?.Log<TraceEntry>(entry.LogLevel, default(EventId), entry, null, (e, ex) =>
                    {
                        if (TraceLogger.DefaultFormatTraceEntry == null) { return e.ToString(); }
                        var ret = TraceLogger.DefaultFormatTraceEntry.FormatTraceEntry(e, ex);
                        return ret;
                    });
                }
                else
                {
                    var entry = new TraceEntry() { TraceEventType = TraceEventType.Stop, LogLevel = this.LogLevel, TraceSource = this.TraceSource, Message = null, Properties = this.Properties, Source = this.Source, Category = this.Category, SourceLevel = this.SourceLevel, CodeSectionBase = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
                    TraceLogger._pendingEntries.Enqueue(entry);
                    // if (TraceLogger._isInitializeComplete.Value == false && TraceLogger._isInitializing.Value == false) { TraceLogger.InitConfiguration(null); }
                }

            }
            finally { CodeSectionBase.Current.Value = Caller; }
        }

        public override object Clone() { return new CodeSectionScope(this); }

        #region GetEntrylogger
        private static ILogger GetEntrylogger(ref TraceEntry entry)
        {
            var t = entry.CodeSectionBase?.T;
            Type loggerType = typeof(ILogger<>);
            loggerType = loggerType.MakeGenericType(new[] { t });

            var services = TraceLogger.Services;
            var logger = default(ILogger);
            try { logger = services.GetRequiredService(loggerType) as ILogger; }
            catch (Exception) { }

            //var logger = services.GetRequiredService(loggerType) as ILogger;
            //var loggerFactory = TraceLogger.LoggerFactory;
            //var logger = loggerFactory.CreateLogger(loggerType);

            return logger;
        }
        #endregion
        #region getOperationInfo
        public static (string, int) getOperationInfo()
        {
            string operationID = null;
            try
            {
                var operationContext = CodeSectionBase.OperationContext.Value;
                //var operationContext = CallContext.LogicalGetData("OperationContext") as IOperationContext;
                if (operationContext != null && !string.IsNullOrEmpty(operationContext.RequestContext?.RequestId))
                {
                    return (operationContext?.RequestContext?.RequestId, operationContext?.RequestContext != null ? operationContext.RequestContext.RequestDept : 0);
                }
                if (string.IsNullOrEmpty(operationID)) { operationID = NanoId.Generate(size: 12); }
            }
            catch (Exception ex) { operationID = ex.Message; }
            return (operationID, 0);
        }
        #endregion
        #region Min
        int Min(int a, int b) { return a < b ? a : b; }
        #endregion

        private Type GetType<T>(T t) { return typeof(T); }

        [Obsolete("Obsolete method, please, use LogDebug method instead")]
        void ICodeSectionLogger.Trace(object obj, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogTrace(obj, category, properties, source, disableCRLFReplace); }
#if NET6_0_OR_GREATER
        [Obsolete("Obsolete method, please, use LogDebug method instead")]
        void ICodeSectionLogger.Trace(ref TraceLoggerInterpolatedStringHandler message, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogTrace(ref message, category, properties, source, disableCRLFReplace); }
        [Obsolete("Obsolete method, please, use LogDebug method instead")]
        void ICodeSectionLogger.Trace(string message, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogTrace(message, category, properties, source, disableCRLFReplace); }
#else
        [Obsolete("Obsolete method, please, use LogDebug method instead")]
        void ICodeSectionLogger.Trace(NonFormattableString message, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogTrace(message, category, properties, source, disableCRLFReplace); }
        [Obsolete("Obsolete method, please, use LogDebug method instead")]
        void ICodeSectionLogger.Trace(FormattableString message, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogTrace(message, category, properties, source, disableCRLFReplace); }
#endif
        [Obsolete("Obsolete method, please, use LogDebug method instead")]
        public void Trace(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false) { this.LogTrace(getMessage, category, properties, source, disableCRLFReplace); }

        [Obsolete("Obsolete method, please, use LogDebug method instead")]
        void ICodeSectionLogger.Debug(object obj, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogDebug(obj, category, properties, source, disableCRLFReplace); }
#if NET6_0_OR_GREATER
        [Obsolete("Obsolete method, please, use LogDebug method instead")]
        void ICodeSectionLogger.Debug([InterpolatedStringHandlerArgument("")] ref DebugLoggerInterpolatedStringHandler message, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogDebug(ref message, category, properties, source, disableCRLFReplace); }
        [Obsolete("Obsolete method, please, use LogDebug method instead")]
        void ICodeSectionLogger.Debug(string message, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogDebug(message, category, properties, source, disableCRLFReplace); }
#else
        [Obsolete("Obsolete method, please, use LogDebug method instead")]
        void ICodeSectionLogger.Debug(NonFormattableString message, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogDebug(message, category, properties, source, disableCRLFReplace); }
        [Obsolete("Obsolete method, please, use LogDebug method instead")]
        void ICodeSectionLogger.Debug(FormattableString message, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogDebug(message, category, properties, source, disableCRLFReplace); }
#endif
        [Obsolete("Obsolete method, please, use LogDebug method instead")]
        public void Debug(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false) { this.LogDebug(getMessage, category, properties, source, disableCRLFReplace); }

#if NET6_0_OR_GREATER
        [Obsolete("Obsolete method, please, use LogInformation method instead")]
        void ICodeSectionLogger.Information([InterpolatedStringHandlerArgument("")] ref InformationLoggerInterpolatedStringHandler message, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogInformation(ref message, category, properties, source, disableCRLFReplace); }
        [Obsolete("Obsolete method, please, use LogInformation method instead")]
        void ICodeSectionLogger.Information(string message, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogInformation(message, category, properties, source, disableCRLFReplace); }
#else
        [Obsolete("Obsolete method, please, use LogInformation method instead")]
        void ICodeSectionLogger.Information(NonFormattableString message, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogInformation(message, category, properties, source, disableCRLFReplace); }
        [Obsolete("Obsolete method, please, use LogInformation method instead")]
        void ICodeSectionLogger.Information(FormattableString message, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogInformation(message, category, properties, source, disableCRLFReplace); }
#endif
        [Obsolete("Obsolete method, please, use LogInformation method instead")]
        void ICodeSectionLogger.Information(Func<string> getMessage, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogInformation(getMessage, category, properties, source, disableCRLFReplace); }

#if NET6_0_OR_GREATER
        [Obsolete("Obsolete method, please, use LogWarning method instead")]
        void ICodeSectionLogger.Warning([InterpolatedStringHandlerArgument("")] ref WarningLoggerInterpolatedStringHandler message, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogWarning(ref message, category, properties, source, disableCRLFReplace); }
        [Obsolete("Obsolete method, please, use LogWarning method instead")]
        void ICodeSectionLogger.Warning(string message, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogWarning(message, category, properties, source, disableCRLFReplace); }
#else
        [Obsolete("Obsolete method, please, use LogWarning method instead")]
        void ICodeSectionLogger.Warning(NonFormattableString message, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogWarning(message, category, properties, source, disableCRLFReplace); }
        [Obsolete("Obsolete method, please, use LogWarning method instead")]
        void ICodeSectionLogger.Warning(FormattableString message, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogWarning(message, category, properties, source, disableCRLFReplace); }
#endif
        [Obsolete("Obsolete method, please, use LogWarning method instead")]
        void ICodeSectionLogger.Warning(Func<string> getMessage, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogWarning(getMessage, category, properties, source, disableCRLFReplace); }

#if NET6_0_OR_GREATER
        [Obsolete("Obsolete method, please, use LogError method instead")]
        void ICodeSectionLogger.Error([InterpolatedStringHandlerArgument("")] ref ErrorLoggerInterpolatedStringHandler message, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogError(ref message, category, properties, source, disableCRLFReplace); }
        [Obsolete("Obsolete method, please, use LogError method instead")]
        void ICodeSectionLogger.Error(string message, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogError(message, category, properties, source, disableCRLFReplace); }
#else
        [Obsolete("Obsolete method, please, use LogError method instead")]
        void ICodeSectionLogger.Error(NonFormattableString message, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogError(message, category, properties, source, disableCRLFReplace); }
        [Obsolete("Obsolete method, please, use LogError method instead")]
        void ICodeSectionLogger.Error(FormattableString message, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogError(message, category, properties, source, disableCRLFReplace); }
#endif
        [Obsolete("Obsolete method, please, use LogError method instead")]
        void ICodeSectionLogger.Error(Func<string> getMessage, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogError(getMessage, category, properties, source, disableCRLFReplace); }

        [Obsolete("Obsolete method, please, use LogException method instead")]
        void ICodeSectionLogger.Exception(Exception exception, string category, IDictionary<string, object> properties, string source, bool disableCRLFReplace) { this.LogException(exception, category, properties, source, disableCRLFReplace); }
    }
}
