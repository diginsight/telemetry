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
#endregion

namespace Common
{
    public class CodeSectionBase : ICodeSection, IDisposable, ICloneable
    {
        #region internal state
        public static Stopwatch _stopwatch = TraceLogger.Stopwatch;
        #endregion

        public bool _isLogEnabled { get; set; } = true;
        public bool _overrideOperationIdEnabled { get; set; } = true;
        public bool? _showNestedFlow { get; set; }
        public int? _maxMessageLevel { get; set; }
        public int? _maxMessageLen { get; set; }
        public int? _maxMessageLenError { get; set; }
        public int? _maxMessageLenWarning { get; set; }
        public int? _maxMessageLenInfo { get; set; }
        public int? _maxMessageLenVerbose { get; set; }
        public int? _maxMessageLenDebug { get; set; }

        public IClassConfigurationGetter ClassConfigurationGetter { get; set; }
        //public ITraceLoggerMinimumLevel TraceLoggerMinimumLevelService { get; set; }
        //public IScopedConfiguration ScopedConfiguration { get; set; }

        public ICodeSection Caller { get; set; }
        public int NestingLevel { get; set; }
        public int OperationDept { get; set; }
        public object Payload { get; set; }
        // public object Exception { get; set; }
        public object Result { get; set; }
        public string Name { get; set; }
        public string MemberName { get; set; }
        public string SourceFilePath { get; set; }
        public int SourceLineNumber { get; set; }
        public string Source { get; set; }
        public string Category { get; set; }

        public long CallStartMilliseconds { get; set; }
        public long CallStartTicks { get; set; }
        public DateTime SystemStartTime { get; set; }
        public string OperationID { get; set; }
        public bool IsDisposed { get; set; }
        public bool DisableStartEndTraces { get; set; }

        public Type T { get; set; }
        public string TypeName { get; set; }
        public string ClassName { get; set; }
        public Assembly Assembly { get; set; }

        public TraceSource TraceSource { get; set; }
        public TraceEventType TraceEventType { get; set; }
        public IModuleContext ModuleContext { get; set; }
        public SourceLevels SourceLevel { get; set; }
        public LogLevel LogLevel { get; set; }
        public LogLevel MinimumLogLevel { get; set; }
        public IDictionary<string, object> Properties { get; set; }
        public bool IsInnerScope { get; set; }
        public bool PublishMetrics { get; set; }
        public bool PublishFlow { get; set; }
        public ICodeSection InnerScope { get; set; }

        public static AsyncLocal<ICodeSection> Current { get; set; } = new AsyncLocal<ICodeSection>();
        public static AsyncLocal<IOperationContext> OperationContext { get; set; } = new AsyncLocal<IOperationContext>();

        public ICodeSection GetInnerSection()
        {
            if (InnerScope == null) { InnerScope = this.Clone() as CodeSectionBase; InnerScope.IsInnerScope = true; }
            return InnerScope;
        }
        public virtual void Dispose()
        {
            if (IsDisposed) { return; }
            IsDisposed = true;
        }
        public virtual object Clone()
        {
            return null;
        }
    }
    public class CodeSectionSurrogate
    {
        public int NestingLevel { get; set; }
        public int OperationDept { get; set; }
        public object Payload { get; set; }
        //public object Exception { get; set; }
        public object Result { get; set; }
        public string Name { get; set; }
        public string MemberName { get; set; }
        public string SourceFilePath { get; set; }
        public int SourceLineNumber { get; set; }
        public bool DisableStartEndTraces { get; set; }
        //public Type T { get; set; }
        public string TypeName { get; set; }
        public string TypeFullName { get; set; }
        //public Assembly Assembly { get; set; }
        public string AssemblyName { get; set; }
        public string AssemblyFullName { get; set; }
        //public TraceSource TraceSource;
        public string TraceSourceName;
        public TraceEventType TraceEventType;
        // public IModuleContext ModuleContext { get; set; }
        public SourceLevels SourceLevel { get; set; }
        public LogLevel LogLevel { get; set; }
        public IDictionary<string, object> Properties { get; set; }
        public string Source { get; set; }
        public string Category { get; set; }
        public long CallStartMilliseconds { get; set; }
        public DateTime SystemStartTime { get; set; }
        public string OperationID { get; set; }
        public bool IsInnerScope { get; set; }
    }
    public class CodeSectionInfo
    {
        public object Payload { get; set; }
        public string Name { get; set; }
        public string MemberName { get; set; }
        public string SourceFilePath { get; set; }
        public int SourceLineNumber { get; set; }
        public long CallStartMilliseconds { get; set; }
        public DateTimeOffset? CallStart { get; set; }
        public DateTimeOffset? CallEnd { get; set; }
        public int NestingLevel { get; set; }
        public Type T { get; set; }
    }
    public class ProcessInfo
    {
        public string ProcessID { get; set; }
        public string ProcessName { get; set; }
        public Assembly Assembly { get; set; }
        public Process Process { get; set; }
        public Thread Thread { get; set; }
        public int ThreadID { get; set; }
    }
    public class SystemInfo
    {
        public string Server { get; set; }
    }
    public class RequestContext : IRequestContext
    {
        public string Method { get; set; }
        public string Path { get; set; }
        public string QueryString { get; set; }
        public string ContentType { get; set; }
        public long? ContentLength { get; set; }
        public string Protocol { get; set; }
        public string PathBase { get; set; }
        public string Host { get; set; }
        public bool IsHttps { get; set; }
        public string Scheme { get; set; }
        public bool HasFormContentType { get; set; }
        public IList<KeyValuePair<string, string>> Headers { get; set; }
        public string TypeName { get; set; }
        public string AssemblyName { get; set; }
        public string Layer { get; set; }
        public string Area { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string RequestId { get; set; }
        public int RequestDept { get; set; }
        public string ServiceName { get; set; }
        public string OperationName { get; set; }
        public string RequestDescription { get; set; }
        public DateTimeOffset RequestStart { get; set; }
        public DateTimeOffset? RequestEnd { get; set; }
        public object Input { get; set; }
        public object Output { get; set; }
        public string ProfileServiceURL { get; set; }
    }
    public class BusinessContext : IBusinessContext
    {
        public string Branch { get; set; }
    }
    public class ModuleContext : IModuleContext
    {
        #region .ctor
        public ModuleContext(Assembly assembly)
        {
            this.Assembly = assembly;
        }
        #endregion

        public bool? ShowNestedFlow { get; set; }
        public int? MaxMessageLevel { get; set; }
        public int? MaxMessageLen { get; set; }
        public int? MaxMessageLenError { get; set; }
        public int? MaxMessageLenWarning { get; set; }
        public int? MaxMessageLenInfo { get; set; }
        public int? MaxMessageLenVerbose { get; set; }
        public int? MaxMessageLenDebug { get; set; }
        public DateTimeOffset? LogggingSettingsCreationDate { get; set; }
        public Assembly Assembly { get; set; }
        public ConcurrentDictionary<string, object> Properties { get; set; } = new ConcurrentDictionary<string, object>();
        public void SetProperty(string name, object val)
        {
            this.Properties[name] = val;
            // map explicit properties
            //var cd = val as ChiaveDescrizione;
            //switch (name)
            //{
            //    case "Configuration.LoggingSettings:MaxMessageLevel":
            //        this.MaxMessageLevel = !string.IsNullOrEmpty(cd?.Valore) ? (int?)ConfigurationManagerCommon.GetValue(cd.Valore, Trace.CONFIGDEFAULT_MAXMESSAGELEVEL, null) : null;
            //        break;
            //};
        }
    }
    public sealed class NonFormattableString
    {
        public NonFormattableString(string arg)
        {
            Value = arg;
        }

        public string Value { get; }

        public static implicit operator NonFormattableString(string arg) { return new NonFormattableString(arg); }

        public static implicit operator NonFormattableString(FormattableString arg) { throw new InvalidOperationException(); }
    }
}
