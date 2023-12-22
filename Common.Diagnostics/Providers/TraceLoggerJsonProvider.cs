#region using
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
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
    public class TraceLoggerJsonProvider : ILoggerProvider, IFormatTraceEntry
    {
        #region const
        public const string CONFIGSETTING_CRREPLACE = "CRReplace"; public const string CONFIGDEFAULT_CRREPLACE = "\\r";
        public const string CONFIGSETTING_LFREPLACE = "LFReplace"; public const string CONFIGDEFAULT_LFREPLACE = "\\n";
        public const string CONFIGSETTING_TIMESTAMPFORMAT = "TimestampFormat"; public const string CONFIGDEFAULT_TIMESTAMPFORMAT = "HH:mm:ss.fff"; // dd/MM/yyyy
        public const string CONFIGSETTING_FLUSHONWRITE = "FlushOnWrite"; public const bool CONFIGDEFAULT_FLUSHONWRITE = false;
        public const string CONFIGSETTING_WRITESTARTUPENTRIES = "WriteStartupEntries"; public const bool CONFIGDEFAULT_WRITESTARTUPENTRIES = true;
        public const string CONFIGSETTING_MERGEMESSAGEANDPAYLOAD = "MergeMessageAndPayload"; public const bool CONFIGDEFAULT_MERGEMESSAGEANDPAYLOAD = true;
        #endregion
        #region internal state
        private static Type T = typeof(TraceLoggerJsonProvider);
        private static readonly string _traceSourceName = "TraceSource";
        public static Func<string, string> CRLF2Space = (string s) => { return s?.Replace("\r", " ")?.Replace("\n", " "); };
        public static Func<string, string> CRLF2Encode = (string s) => { return s?.Replace("\r", "\\r")?.Replace("\n", "\\n"); };
        public string Name { get; set; }
        public string ConfigurationSuffix { get; set; }
        bool _lastWriteContinuationEnabled;
        public string _CRReplace, _LFReplace;
        public string _timestampFormat;
        public bool _showNestedFlow, _showTraceCost, _flushOnWrite;
        public string _traceDeltaDefault;
        public bool _writeStartupEntries;
        public bool _mergeMessageAndPayload;
        public TraceEventType _allowedEventTypes = TraceEventType.Critical | TraceEventType.Error | TraceEventType.Warning | TraceEventType.Information | TraceEventType.Verbose | TraceEventType.Start | TraceEventType.Stop | TraceEventType.Suspend | TraceEventType.Resume | TraceEventType.Transfer;
        TraceEntry lastWrite = default(TraceEntry);
        ILoggerProvider _provider;

        public static ConcurrentDictionary<string, object> Properties { get; set; } = new ConcurrentDictionary<string, object>();
        public IList<ILogger> Listeners { get; } = new List<ILogger>();
        #endregion

        public TraceLoggerJsonProvider() { }
        public TraceLoggerJsonProvider(IConfiguration configuration)
        {
            TraceLogger.InitConfiguration(configuration);
        }

        public void AddProvider(ILoggerProvider provider)
        {
            using (var scope = TraceLogger.BeginMethodScope<TraceLoggerJsonProvider>())
            {
                if (string.IsNullOrEmpty(ConfigurationSuffix))
                {
                    var prefix = provider?.GetType()?.Name?.Split('.')?.Last();
                    this.ConfigurationSuffix = prefix;
                }

                var classConfigurationGetter = new ClassConfigurationGetter<TraceLoggerJsonProvider>(TraceLogger.Configuration);

                //_CRReplace = ConfigurationHelper.GetClassSetting<TraceLoggerJsonProvider, string>(CONFIGSETTING_CRREPLACE, CONFIGDEFAULT_CRREPLACE, CultureInfo.InvariantCulture, this.ConfigurationSuffix);
                _CRReplace = classConfigurationGetter.Get(CONFIGSETTING_CRREPLACE, CONFIGDEFAULT_CRREPLACE);
                //_LFReplace = ConfigurationHelper.GetClassSetting<TraceLoggerJsonProvider, string>(CONFIGSETTING_LFREPLACE, CONFIGDEFAULT_LFREPLACE, CultureInfo.InvariantCulture, this.ConfigurationSuffix);  // ConfigurationHelper.GetSetting<int>(CONFIGSETTING_LFREPLACE, CONFIGDEFAULT_LFREPLACE);
                _LFReplace = classConfigurationGetter.Get(CONFIGSETTING_LFREPLACE, CONFIGDEFAULT_LFREPLACE);
                //_timestampFormat = ConfigurationHelper.GetClassSetting<TraceLoggerJsonProvider, string>(CONFIGSETTING_TIMESTAMPFORMAT, CONFIGDEFAULT_TIMESTAMPFORMAT, CultureInfo.InvariantCulture, this.ConfigurationSuffix);  // ConfigurationHelper.GetSetting<int>(CONFIGSETTING_TIMESTAMPFORMAT, CONFIGDEFAULT_TIMESTAMPFORMAT);
                _timestampFormat = classConfigurationGetter.Get(CONFIGSETTING_TIMESTAMPFORMAT, CONFIGDEFAULT_TIMESTAMPFORMAT);
                //_writeStartupEntries = ConfigurationHelper.GetClassSetting<TraceLoggerFormatProvider, bool>(CONFIGSETTING_WRITESTARTUPENTRIES, CONFIGDEFAULT_WRITESTARTUPENTRIES, CultureInfo.InvariantCulture, this.ConfigurationSuffix);
                _writeStartupEntries = classConfigurationGetter.Get(CONFIGSETTING_WRITESTARTUPENTRIES, CONFIGDEFAULT_WRITESTARTUPENTRIES);
                _mergeMessageAndPayload = classConfigurationGetter.Get(CONFIGSETTING_MERGEMESSAGEANDPAYLOAD, CONFIGDEFAULT_MERGEMESSAGEANDPAYLOAD);

                var thicksPerMillisecond = TraceLogger.Stopwatch.ElapsedTicks / TraceLogger.Stopwatch.ElapsedMilliseconds;
                string fileName = null, workingDirectory = null;
                try { fileName = TraceLogger.CurrentProcess?.MainModule?.FileName; } catch { };
                try { workingDirectory = Directory.GetCurrentDirectory(); } catch { };

                scope.LogInformation($"Starting {this.GetType().Name} for: ProcessName: '{TraceLogger.ProcessName}', ProcessId: '{TraceLogger.ProcessId}', FileName: '{fileName}', WorkingDirectory: '{workingDirectory}', EntryAssemblyFullName: '{TraceLogger.EntryAssembly?.FullName}', ImageRuntimeVersion: '{TraceLogger.EntryAssembly?.ImageRuntimeVersion}', Location: '{TraceLogger.EntryAssembly?.Location}', thicksPerMillisecond: '{thicksPerMillisecond}'{Environment.NewLine}", null, new Dictionary<string, object>() { { "MaxMessageLen", 0 } }); // "init"
                _provider = provider;
            }
        }
        public ILogger CreateLogger(string categoryName)
        {
            var innerLogger = _provider.CreateLogger(categoryName);
            var logger = new TraceLogger(this, categoryName); // TODO: use provider to get config
            logger.Listeners.Add(innerLogger);

            return logger;
        }
        public void Dispose()
        {
            ;
        }

        public string FormatTraceEntry(TraceEntry entry, Exception ex)
        {
            var traceSurrogate = GetTraceSurrogate(entry);
            var entryJson = SerializationHelper.SerializeJson(traceSurrogate);
            return entryJson;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TraceEntrySurrogate GetTraceSurrogate(TraceEntry entry)
        {
            var codeSection = entry.CodeSectionBase;
            var message = entry.GetMessage != null ? entry.GetMessage() : entry.Message;
            if (_mergeMessageAndPayload && entry.MessageObject != null)
            {
                var payloadJson = entry.MessageObject.GetLogString();
                message = payloadJson;
            }
            var payload = codeSection.Payload is Func<object> func ? func() : codeSection.Payload;
            if (_mergeMessageAndPayload && payload != null)
            {
                var payloadJson = payload.GetLogString();
                payload = payloadJson;
            }

            return new TraceEntrySurrogate()
            {
                TraceEventType = entry.TraceEventType,
                TraceEventTypeDesc = entry.TraceEventType.ToString(),
                TraceSourceName = entry.TraceSource?.Name,
                LogLevel = entry.LogLevel,
                Message = message,
                MessageObject = _mergeMessageAndPayload == false ? entry.MessageObject : null,
                Properties = entry.Properties,
                Source = entry.Source,
                Category = entry.Category,
                SourceLevel = entry.SourceLevel,
                ElapsedMilliseconds = entry.ElapsedMilliseconds,
                Timestamp = entry.Timestamp,
                Exception = entry.Exception,
                ThreadID = entry.ThreadID,
                ApartmentState = entry.ApartmentState,
                DisableCRLFReplace = entry.DisableCRLFReplace,
                CodeSection = codeSection != null ? new CodeSectionSurrogate()
                {
                    NestingLevel = codeSection.NestingLevel,
                    OperationDept = codeSection.OperationDept,
                    Payload = payload,
                    Result = codeSection.Result,
                    Name = codeSection.Name,
                    MemberName = codeSection.MemberName,
                    SourceFilePath = codeSection.SourceFilePath,
                    SourceLineNumber = codeSection.SourceLineNumber,
                    DisableStartEndTraces = codeSection.DisableStartEndTraces,
                    TypeName = codeSection.T?.Name,
                    TypeFullName = codeSection.T?.FullName,
                    AssemblyName = codeSection.Assembly?.GetName()?.Name,
                    AssemblyFullName = codeSection.Assembly?.FullName,
                    TraceSourceName = codeSection.TraceSource?.Name,
                    TraceEventType = codeSection.TraceEventType,
                    SourceLevel = codeSection.SourceLevel,
                    LogLevel = codeSection.LogLevel,
                    Properties = codeSection.Properties,
                    Source = codeSection.Source,
                    Category = codeSection.Category,
                    CallStartMilliseconds = codeSection.CallStartMilliseconds,
                    SystemStartTime = codeSection.SystemStartTime,
                    OperationID = codeSection.OperationID,
                    IsInnerScope = codeSection.IsInnerScope
                } : null
            };
        }
        #region Min
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int Min(int a, int b) { return a < b ? a : b; }
        #endregion
    }

    [ProviderAlias("DiginsightJsonLog4Net")]
    [ConfigurationName("DiginsightJsonLog4Net")]
    public class DiginsightJsonLog4NetProvider : TraceLoggerJsonProvider
    {
        #region .ctor
        public DiginsightJsonLog4NetProvider() { }
        public DiginsightJsonLog4NetProvider(IConfiguration configuration) : base(configuration) { }
        #endregion
    }

    [ProviderAlias("DiginsightJsonApplicationInsights")]
    [ConfigurationName("DiginsightJsonApplicationInsights")]
    public class DiginsightJsonApplicationInsightsProvider : TraceLoggerJsonProvider
    {
        #region .ctor
        public DiginsightJsonApplicationInsightsProvider() { }
        public DiginsightJsonApplicationInsightsProvider(IConfiguration configuration) : base(configuration) { }
        #endregion
    }

    [ProviderAlias("DiginsightJsonConsole")]
    [ConfigurationName("DiginsightJsonConsole")]
    public class DiginsightJsonConsoleProvider : TraceLoggerJsonProvider
    {
        #region .ctor
        public DiginsightJsonConsoleProvider() { }
        public DiginsightJsonConsoleProvider(IConfiguration configuration) : base(configuration) { }
        #endregion
    }

    [ProviderAlias("DiginsightJsonDebug")]
    [ConfigurationName("DiginsightJsonDebug")]
    public class DiginsightJsonDebugProvider : TraceLoggerJsonProvider
    {
        #region .ctor
        public DiginsightJsonDebugProvider() { }
        public DiginsightJsonDebugProvider(IConfiguration configuration) : base(configuration) { }
        #endregion
    }

}
