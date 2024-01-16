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
    public class CodeSection : CodeSectionBase, IDisposable, ICloneable, ICodeSectionLogger
    { 
        #region .ctor
        static CodeSection() { }
        public CodeSection(CodeSection pCopy)
        {
            this.Name = pCopy.Name;
            this.Payload = pCopy.Payload;
            this.TraceSource = pCopy.TraceSource;
            this.SourceLevel = pCopy.SourceLevel;
            this.MemberName = pCopy.MemberName;
            this.SourceFilePath = pCopy.SourceFilePath;
            this.SourceLineNumber = pCopy.SourceLineNumber;
            this.DisableStartEndTraces = true;
            this.T = pCopy.T;
            this.Assembly = pCopy.Assembly;
            this.Category = pCopy.Category;
            this.Source = pCopy.Source;
            this.CallStartMilliseconds = pCopy.CallStartMilliseconds;

            Caller = CodeSectionBase.Current.Value;

            this.NestingLevel = pCopy.NestingLevel;
            this.OperationID = pCopy.OperationID;
            this.OperationDept = pCopy.OperationDept;

            this.ModuleContext = pCopy.ModuleContext;

        }
        public CodeSection(object pthis, string name = null, object payload = null, TraceSource traceSource = null, SourceLevels sourceLevel = SourceLevels.Verbose,
                           string category = null, IDictionary<string, object> properties = null, string source = null, long startTicks = 0, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
                           : this(pthis.GetType(), name, payload, traceSource, sourceLevel, category, properties, source, startTicks, memberName, sourceFilePath, sourceLineNumber)
        { }

        public CodeSection(Type type, string name = null, object payload = null, TraceSource traceSource = null, SourceLevels sourceLevel = SourceLevels.Verbose,
                           string category = null, IDictionary<string, object> properties = null, string source = null, long startTicks = 0, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0, bool disableStartEndTraces = false)
        {
            this.Name = name;
            this.Payload = payload;
            this.TraceSource = traceSource;
            this.SourceLevel = sourceLevel;
            this.MemberName = memberName;
            this.SourceFilePath = sourceFilePath;
            this.SourceLineNumber = sourceLineNumber;
            this.DisableStartEndTraces = disableStartEndTraces;
            this.T = type;
            this.Assembly = type?.Assembly;
            this.Category = category;
            if (string.IsNullOrEmpty(source))
            {
                var assemplyName = this.Assembly?.GetName();
                source = assemplyName?.Name;
            }

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
                OperationID = Caller.OperationID;
                OperationDept = Caller.OperationDept;
                if (string.IsNullOrEmpty(OperationID)) { (this.OperationID, this.OperationDept) = getOperationInfo(); }
            }
            else
            {
                (string operationID, int operationDept) = getOperationInfo();
                NestingLevel = 0;
                OperationID = operationID;
                OperationDept = operationDept;
            }
            this.ModuleContext = this.Assembly != null ? TraceManager.GetModuleContext(this.Assembly) : null;

            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Start) || this.DisableStartEndTraces == true) { return; }

            var entry = new TraceEntry()
            {
                TraceEventType = TraceEventType.Start,
                TraceSource = this.TraceSource,
                Message = null,
                Properties = properties,
                Source = source,
                Category = category,
                SourceLevel = sourceLevel,
                CodeSection = this,
                Thread = Thread.CurrentThread,
                ThreadID = Thread.CurrentThread.ManagedThreadId,
                ApartmentState = Thread.CurrentThread.GetApartmentState(),
                ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds,
                TraceStartTicks = startTicks
            };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                // traceSource.TraceData()
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                // Trace.WriteLine()
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0)
                {
                    foreach (TraceListener listener in System.Diagnostics.Trace.Listeners)
                    {
                        try
                        {
                            listener.WriteLine(entry);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }
        }
        #endregion

        public void Trace(object obj, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Verbose)) { return; }

            var message = obj.GetLogString();

            var entry = new TraceEntry() { Message = message, TraceEventType = TraceEventType.Verbose, SourceLevel = SourceLevels.Verbose, Properties = properties, Source = source ?? this.Source, Category = category, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }
        }
#if NET6_0_OR_GREATER
        public void Trace([InterpolatedStringHandlerArgument("")] ref TraceLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Verbose)) { return; }

            var entry = new TraceEntry()
            {
                //MessageFormat = message.FormatTemplate?.ToString(),
                //MessageArgs = message.FormatParameters,
                GetMessage = new Func<string>(message.FinalString.ToString),
                TraceEventType = TraceEventType.Verbose,
                SourceLevel = SourceLevels.Verbose,
                Properties = properties,
                Source = source ?? this.Source,
                Category = category,
                CodeSection = this,
                Thread = Thread.CurrentThread,
                ThreadID = Thread.CurrentThread.ManagedThreadId,
                ApartmentState = Thread.CurrentThread.GetApartmentState(),
                DisableCRLFReplace = disableCRLFReplace,
                ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds,
                TraceStartTicks = startTicks
            };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }
        }
        public void Trace(string message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Verbose)) { return; }

            var entry = new TraceEntry() { Message = message, TraceEventType = TraceEventType.Verbose, SourceLevel = SourceLevels.Verbose, Properties = properties, Source = source ?? this.Source, Category = category, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }
        }
#else
        public void Trace(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Verbose)) { return; }

            var entry = new TraceEntry() { Message = message.Value, TraceEventType = TraceEventType.Verbose, SourceLevel = SourceLevels.Verbose, Properties = properties, Source = source ?? this.Source, Category = category, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }
        }
        public void Trace(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Verbose)) { return; }

            try
            {
                var entry = new TraceEntry() { Message = string.Format(message.Format, message.GetArguments()), TraceEventType = TraceEventType.Verbose, SourceLevel = SourceLevels.Verbose, Properties = properties, Source = source ?? this.Source, Category = category, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
                if (!TraceManager._lockListenersNotifications.Value)
                {
                    if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                    if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                }
                else
                {
                    TraceManager._pendingEntries.Enqueue(entry);
                    if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
                }
            }
            catch (Exception) { }
        }
#endif
        public void Trace(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Verbose)) { return; }

            try
            {
                var entry = new TraceEntry() { GetMessage = getMessage, TraceEventType = TraceEventType.Verbose, SourceLevel = SourceLevels.Verbose, Properties = properties, Source = source ?? this.Source, Category = category, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
                if (!TraceManager._lockListenersNotifications.Value)
                {
                    if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                    if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                }
                else
                {
                    TraceManager._pendingEntries.Enqueue(entry);
                    if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
                }
            }
            catch (Exception) { }
        }

        public void Debug(object obj, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Verbose)) { return; }

            var message = obj.GetLogString();

            var entry = new TraceEntry() { Message = message, TraceEventType = TraceEventType.Verbose, SourceLevel = SourceLevels.Verbose, Properties = properties, Source = source ?? this.Source, Category = category, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }
        }
#if NET6_0_OR_GREATER
        public void Debug([InterpolatedStringHandlerArgument("")] ref DebugLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Verbose)) { return; }

            var entry = new TraceEntry()
            {
                //MessageFormat = message.FormatTemplate?.ToString(),
                //MessageArgs = message.FormatParameters,
                GetMessage = new Func<string>(message.FinalString.ToString),
                TraceEventType = TraceEventType.Verbose,
                SourceLevel = SourceLevels.Verbose,
                Properties = properties,
                Source = source ?? this.Source,
                Category = category,
                CodeSection = this,
                Thread = Thread.CurrentThread,
                ThreadID = Thread.CurrentThread.ManagedThreadId,
                ApartmentState = Thread.CurrentThread.GetApartmentState(),
                DisableCRLFReplace = disableCRLFReplace,
                ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds,
                TraceStartTicks = startTicks
            };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }
        }
        public void Debug(string message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Verbose)) { return; }

            var entry = new TraceEntry() { Message = message, TraceEventType = TraceEventType.Verbose, SourceLevel = SourceLevels.Verbose, Properties = properties, Source = source ?? this.Source, Category = category, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }
        }
#else
        public void Debug(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Verbose)) { return; }

            var entry = new TraceEntry() { Message = message.Value, TraceEventType = TraceEventType.Verbose, SourceLevel = SourceLevels.Verbose, Properties = properties, Source = source ?? this.Source, Category = category, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }
        }
        public void Debug(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Verbose)) { return; }

            try
            {
                var entry = new TraceEntry() { Message = string.Format(message.Format, message.GetArguments()), TraceEventType = TraceEventType.Verbose, SourceLevel = SourceLevels.Verbose, Properties = properties, Source = source ?? this.Source, Category = category, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
                if (!TraceManager._lockListenersNotifications.Value)
                {
                    if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                    if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                }
                else
                {
                    TraceManager._pendingEntries.Enqueue(entry);
                    if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
                }
            }
            catch (Exception) { }
        }
#endif
        public void Debug(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Verbose)) { return; }

            try
            {
                var entry = new TraceEntry() { GetMessage = getMessage, TraceEventType = TraceEventType.Verbose, SourceLevel = SourceLevels.Verbose, Properties = properties, Source = source ?? this.Source, Category = category, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
                if (!TraceManager._lockListenersNotifications.Value)
                {
                    if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                    if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                }
                else
                {
                    TraceManager._pendingEntries.Enqueue(entry);
                    if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
                }
            }
            catch (Exception) { }
        }

#if NET6_0_OR_GREATER
        public void Information([InterpolatedStringHandlerArgument("")] ref InformationLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Information)) { return; }

            var entry = new TraceEntry()
            {
                //MessageFormat = message.FormatTemplate?.ToString(),
                //MessageArgs = message.FormatParameters,
                GetMessage = new Func<string>(message.FinalString.ToString),
                TraceEventType = TraceEventType.Information,
                SourceLevel = SourceLevels.Information,
                TraceSource = this.TraceSource,
                Properties = properties,
                Source = source ?? this.Source,
                Category = category,
                CodeSection = this,
                Thread = Thread.CurrentThread,
                ThreadID = Thread.CurrentThread.ManagedThreadId,
                ApartmentState = Thread.CurrentThread.GetApartmentState(),
                DisableCRLFReplace = disableCRLFReplace,
                ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds,
                TraceStartTicks = startTicks
            };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }
        }
        public void Information(string message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Information)) { return; }

            var entry = new TraceEntry() { Message = message, TraceEventType = TraceEventType.Information, SourceLevel = SourceLevels.Information, TraceSource = this.TraceSource, Properties = properties, Source = source ?? this.Source, Category = category, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }
        }
#else
        public void Information(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Information)) { return; }

            var entry = new TraceEntry() { Message = message.Value, TraceEventType = TraceEventType.Information, SourceLevel = SourceLevels.Information, TraceSource = this.TraceSource, Properties = properties, Source = source ?? this.Source, Category = category, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }
        }
        public void Information(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Information)) { return; }

            var entry = new TraceEntry() { Message = string.Format(message.Format, message.GetArguments()), TraceEventType = TraceEventType.Information, SourceLevel = SourceLevels.Information, TraceSource = this.TraceSource, Properties = properties, Source = source ?? this.Source, Category = category, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }
        }
#endif
        public void Information(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Information)) { return; }

            var entry = new TraceEntry() { GetMessage = getMessage, TraceEventType = TraceEventType.Information, SourceLevel = SourceLevels.Information, TraceSource = this.TraceSource, Properties = properties, Source = source ?? this.Source, Category = category, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }
        }

#if NET6_0_OR_GREATER
        public void Warning([InterpolatedStringHandlerArgument("")] ref WarningLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Warning)) { return; }

            var entry = new TraceEntry()
            {
                //MessageFormat = message.FormatTemplate?.ToString(),
                //MessageArgs = message.FormatParameters,
                GetMessage = new Func<string>(message.FinalString.ToString),
                TraceEventType = TraceEventType.Warning,
                SourceLevel = SourceLevels.Warning,
                TraceSource = this.TraceSource,
                Properties = properties,
                Source = source ?? this.Source,
                Category = category,
                CodeSection = this,
                Thread = Thread.CurrentThread,
                ThreadID = Thread.CurrentThread.ManagedThreadId,
                ApartmentState = Thread.CurrentThread.GetApartmentState(),
                DisableCRLFReplace = disableCRLFReplace,
                ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds,
                TraceStartTicks = startTicks
            };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }

        }
        public void Warning(string message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Warning)) { return; }

            var entry = new TraceEntry() { Message = message, TraceEventType = TraceEventType.Warning, SourceLevel = SourceLevels.Warning, TraceSource = this.TraceSource, Properties = properties, Source = source ?? this.Source, Category = category, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }

        }
#else
        public void Warning(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Warning)) { return; }

            var entry = new TraceEntry() { Message = message.Value, TraceEventType = TraceEventType.Warning, SourceLevel = SourceLevels.Warning, TraceSource = this.TraceSource, Properties = properties, Source = source ?? this.Source, Category = category, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }

        }
        public void Warning(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Warning)) { return; }

            var entry = new TraceEntry() { Message = string.Format(message.Format, message.GetArguments()), TraceEventType = TraceEventType.Warning, SourceLevel = SourceLevels.Warning, TraceSource = this.TraceSource, Properties = properties, Source = source ?? this.Source, Category = category, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }
        }
#endif
        public void Warning(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Warning)) { return; }

            var entry = new TraceEntry() { GetMessage = getMessage, TraceEventType = TraceEventType.Warning, SourceLevel = SourceLevels.Warning, TraceSource = this.TraceSource, Properties = properties, Source = source ?? this.Source, Category = category, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }
        }

#if NET6_0_OR_GREATER
        public void Error([InterpolatedStringHandlerArgument("")] ref ErrorLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Error)) { return; }

            var entry = new TraceEntry()
            {
                //MessageFormat = message.FormatTemplate?.ToString(),
                //MessageArgs = message.FormatParameters,
                GetMessage = new Func<string>(message.FinalString.ToString),
                TraceEventType = TraceEventType.Error,
                SourceLevel = SourceLevels.Error,
                TraceSource = this.TraceSource,
                Properties = properties,
                Source = source ?? this.Source,
                Category = category,
                CodeSection = this,
                Thread = Thread.CurrentThread,
                ThreadID = Thread.CurrentThread.ManagedThreadId,
                ApartmentState = Thread.CurrentThread.GetApartmentState(),
                DisableCRLFReplace = disableCRLFReplace,
                ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds,
                TraceStartTicks = startTicks
            };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }
        }
        public void Error(string message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Error)) { return; }

            var entry = new TraceEntry() { Message = message, TraceEventType = TraceEventType.Error, SourceLevel = SourceLevels.Error, TraceSource = this.TraceSource, Properties = properties, Source = source ?? this.Source, Category = category, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }
        }
#else
        public void Error(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Error)) { return; }

            var entry = new TraceEntry() { Message = message.Value, TraceEventType = TraceEventType.Error, SourceLevel = SourceLevels.Error, TraceSource = this.TraceSource, Properties = properties, Source = source ?? this.Source, Category = category, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }
        }
        public void Error(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Error)) { return; }

            var entry = new TraceEntry() { Message = string.Format(message.Format, message.GetArguments()), TraceEventType = TraceEventType.Error, SourceLevel = SourceLevels.Error, TraceSource = this.TraceSource, Properties = properties, Source = source ?? this.Source, Category = category, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }
        }
#endif
        public void Error(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Error)) { return; }

            var entry = new TraceEntry() { GetMessage = getMessage, TraceEventType = TraceEventType.Error, SourceLevel = SourceLevels.Error, TraceSource = this.TraceSource, Properties = properties, Source = source ?? this.Source, Category = category, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), DisableCRLFReplace = disableCRLFReplace, ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }
        }

        public void Exception(Exception exception, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = true)
        {
            var startTicks = TraceManager.Stopwatch.ElapsedTicks;
            if (exception == null) return;
            if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Critical)) { return; }

            var entry = new TraceEntry()
            {
                TraceEventType = TraceEventType.Critical,
                SourceLevel = SourceLevels.Critical,
                TraceSource = this.TraceSource,
                Message = $"Exception: {exception.Message} (InnerException: {exception?.InnerException?.Message ?? "null"})\nStackTrace: {exception.StackTrace}",
                Properties = properties,
                Source = source ?? this.Source,
                Category = category,
                CodeSection = this,
                Thread = Thread.CurrentThread,
                ThreadID = Thread.CurrentThread.ManagedThreadId,
                ApartmentState = Thread.CurrentThread.GetApartmentState(),
                DisableCRLFReplace = disableCRLFReplace,
                ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds,
                TraceStartTicks = startTicks
            };
            if (!TraceManager._lockListenersNotifications.Value)
            {
                if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
            }
            else
            {
                TraceManager._pendingEntries.Enqueue(entry);
                if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
            }
        }

        public override void Dispose()
        {
            var startTicks = TraceLogger.Stopwatch.ElapsedTicks;
            if (IsDisposed) { return; }

            base.Dispose();

            try
            {
                if (TraceSource?.Switch != null && !TraceSource.Switch.ShouldTrace(TraceEventType.Stop) || this.DisableStartEndTraces == true) { return; }
                if (!TraceManager._lockListenersNotifications.Value)
                {
                    var entry = new TraceEntry() { TraceEventType = TraceEventType.Stop, TraceSource = this.TraceSource, Message = null, Properties = this.Properties, Source = this.Source, Category = this.Category, SourceLevel = this.SourceLevel, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
                    if (TraceSource?.Listeners != null && TraceSource.Listeners.Count > 0) { foreach (TraceListener listener in TraceSource.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                    if (System.Diagnostics.Trace.Listeners != null && System.Diagnostics.Trace.Listeners.Count > 0) { foreach (TraceListener listener in System.Diagnostics.Trace.Listeners) { try { listener.WriteLine(entry); } catch (Exception) { } } }
                }
                else
                {
                    var entry = new TraceEntry() { TraceEventType = TraceEventType.Stop, TraceSource = this.TraceSource, Message = null, Properties = this.Properties, Source = this.Source, Category = this.Category, SourceLevel = this.SourceLevel, CodeSection = this, Thread = Thread.CurrentThread, ThreadID = Thread.CurrentThread.ManagedThreadId, ApartmentState = Thread.CurrentThread.GetApartmentState(), ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds, TraceStartTicks = startTicks };
                    TraceManager._pendingEntries.Enqueue(entry);
                    if (TraceManager._isInitializeComplete.Value == false && TraceManager._isInitializing.Value == 0) { TraceManager.Init(SourceLevels.All, null); }
                }

            }
            finally { CodeSectionBase.Current.Value = Caller; }
        }

        public override object Clone() { return new CodeSection(this); }

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
            }
            catch (Exception ex) { operationID = ex.Message; }
            return (operationID, 0);
        }
        #endregion
        #region Min
        int Min(int a, int b) { return a < b ? a : b; }
        #endregion
    }
}
