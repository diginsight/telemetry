using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    #region LogEventLevel
    public enum LogEventLevel
    {
        // Summary: Anything and everything you might want to know about a running block of code.
        Verbose = 0,
        // Summary: Internal system events that aren't necessarily observable from the outside.
        Debug = 1,
        // Summary: The lifeblood of operational intelligence - things happen.
        Information = 2,
        // Summary: Service is degraded or endangered.
        Warning = 3,
        // Summary: Functionality is unavailable, invariants are broken or data is lost.
        Error = 4,
        // Summary: If you have a pager, it goes off when one of these occurs.
        Fatal = 5
    }
    #endregion

    public interface ICodeSection {
        bool _isLogEnabled { get; set; } // = true;
        bool? _showNestedFlow { get; set; }
        int? _maxMessageLevel { get; set; }
        int? _maxMessageLen { get; set; }
        int? _maxMessageLenError { get; set; }
        int? _maxMessageLenWarning { get; set; }
        int? _maxMessageLenInfo { get; set; }
        int? _maxMessageLenVerbose { get; set; }
        int? _maxMessageLenDebug { get; set; }

        ICodeSection Caller { get; set; }
        int NestingLevel { get; set; }
        int OperationDept { get; set; }
        object Payload { get; set; }
        // object Exception { get; set; }
        object Result { get; set; }
        string Name { get; set; }
        string MemberName { get; set; }
        string SourceFilePath { get; set; }
        int SourceLineNumber { get; set; }
        string Source { get; set; }
        string Category { get; set; }

        long CallStartMilliseconds { get; set; }
        long CallStartTicks { get; set; }
        DateTime SystemStartTime { get; set; }
        string OperationID { get; set; }
        bool IsDisposed { get; set; }
        bool DisableStartEndTraces { get; set; }

        Type T { get; set; }
        string TypeName { get; set; }
        string ClassName { get; set; }
        Assembly Assembly { get; set; }

        TraceSource TraceSource { get; set; }
        TraceEventType TraceEventType { get; set; }
        IModuleContext ModuleContext { get; set; }
        SourceLevels SourceLevel { get; set; }
        LogLevel LogLevel { get; set; }
        LogLevel MinimumLogLevel { get; set; }
        IDictionary<string, object> Properties { get; set; }
        bool IsInnerScope { get; set; }
        ICodeSection InnerScope { get; set; }

        ICodeSection GetInnerSection();
    }
    public interface ICodeSectionLogger
    {
        void Trace(object obj, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);
#if NET6_0_OR_GREATER
        void Trace(ref TraceLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);
        void Trace(string message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);
#else
        void Trace(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);
        void Trace(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);
#endif
        void Trace(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);

        void Debug(object obj, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);
#if NET6_0_OR_GREATER
        void Debug(ref DebugLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);
        void Debug(string message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);
#else
        void Debug(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);
        void Debug(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);
#endif
        void Debug(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);

#if NET6_0_OR_GREATER
        void Information(ref InformationLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);
        void Information(string message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);
#else
        void Information(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);
        void Information(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);
#endif
        void Information(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);

#if NET6_0_OR_GREATER
        void Warning(ref WarningLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);
        void Warning(string message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);
#else
        void Warning(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);
        void Warning(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);
#endif
        void Warning(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);

#if NET6_0_OR_GREATER
        void Error(ref ErrorLoggerInterpolatedStringHandler message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);
        void Error(string message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);
#else
        void Error(NonFormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);
        void Error(FormattableString message, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);
#endif
        void Error(Func<string> getMessage, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = false);

        void Exception(Exception exception, string category = null, IDictionary<string, object> properties = null, string source = null, bool disableCRLFReplace = true);
    }

    public interface ISupportFilters
    {
        string Filter { get; set; }
    }

    public interface ISupportInnerListener
    {
        TraceListener InnerListener { get; set; }
    }
    public interface IFormatTraceEntry
    {
        string FormatTraceEntry(TraceEntry entry, Exception ex);
    }
    public interface IRequestContext
    {
        string Method { get; set; }
        string Path { get; set; }
        string QueryString { get; set; }
        string ContentType { get; set; }
        long? ContentLength { get; set; }
        string Protocol { get; set; }
        string PathBase { get; set; }
        string Host { get; set; }
        bool IsHttps { get; set; }
        string Scheme { get; set; }
        bool HasFormContentType { get; set; }
        IList<KeyValuePair<string, string>> Headers { get; set; }
        string TypeName { get; set; }
        string AssemblyName { get; set; }
        string Layer { get; set; }
        string Area { get; set; }
        string Controller { get; set; }
        string Action { get; set; }
        string RequestId { get; set; }
        int RequestDept { get; set; }
        string ServiceName { get; set; }
        string OperationName { get; set; }
        string RequestDescription { get; set; }
        DateTimeOffset RequestStart { get; set; }
        DateTimeOffset? RequestEnd { get; set; }
        object Input { get; set; }
        object Output { get; set; }
        string ProfileServiceURL { get; set; }
    }
    public interface IBusinessContext
    {
        string Branch { get; set; }
    }
    public interface IUserContext
    {
        bool? IsAuthenticated { get; set; }
        string AuthenticationType { get; set; }
        string ImpersonationLevel { get; set; }
        bool? IsAnonymous { get; set; }
        bool? IsGuest { get; set; }
        bool? IsSystem { get; set; }
        IIdentity Identity { get; set; }
    }
    public interface ISessionContext
    {
        string SessionId { get; set; }
        bool? SessionIsAvailable { get; set; }
    }
    public interface ISystemContext
    {
        string ConnectionId { get; set; }
        string ConnectionLocalIpAddress { get; set; }
        int? ConnectionLocalPort { get; set; }
        string ConnectionRemoteIpAddress { get; set; }
        int? ConnectionRemotePort { get; set; }
        string Server { get; set; }
    }
    public interface IOperationContext
    {
        //// REQUEST
        IRequestContext RequestContext { get; set; }
        // USER
        IUserContext UserContext { get; set; }
        // SESSION
        ISessionContext SessionContext { get; set; }
        // SYSTEM
        ISystemContext SystemContext { get; set; }
        // BUSINESS
        IBusinessContext BusinessContext { get; set; }
    }
    public interface IModuleContext
    {
        //bool? ShowNestedFlow { get; set; }
        int? MaxMessageLevel { get; set; }
        int? MaxMessageLen { get; set; }
        int? MaxMessageLenError { get; set; }
        int? MaxMessageLenWarning { get; set; }
        int? MaxMessageLenInfo { get; set; }
        int? MaxMessageLenVerbose { get; set; }
        int? MaxMessageLenDebug { get; set; }
        //DateTimeOffset? LogggingSettingsCreationDate { get; set; }

        Assembly Assembly { get; set; }
        ConcurrentDictionary<string, object> Properties { get; set; }
    }

    public interface ITraceLoggerMinimumLevel {
        LogLevel MinimumLevel { get; set; }
    }
    public interface IScopedConfiguration
    {
        public T? GetValue<T>(string key, T defaultValue);
    }
    public interface IScopedClassConfigurationGetter<TClass>: IScopedConfiguration
    {
        public T? GetValue<T>(string key, T defaultValue);
    }

}
