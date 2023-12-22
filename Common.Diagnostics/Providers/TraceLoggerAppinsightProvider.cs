#region using
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
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
    //public class TraceLoggerApplicationInsights : ILogger
    //{
    //    private bool _trackExceptionsAsExceptionTelemetryEnabled;
    //    //private bool _trackExceptionsAsExceptionTelemetryEnabled;

    //    public IDisposable BeginScope<TState>(TState state)
    //    {
    //        //Console.WriteLine(state);
    //        return null;
    //    }

    //    public bool IsEnabled(LogLevel logLevel)
    //    {
    //        return true;
    //    }

    //    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    //    {
    //        if (formatter == null)
    //        {
    //            //Console.WriteLine(state);
    //        }
    //        else
    //        {
    //            var message = formatter(state, exception);
    //            //Console.Write(message);
    //        }
    //    }
    //}

    public class TraceLoggerApplicationInsightsProvider : ILoggerProvider, IFormatTraceEntry
    {
        #region const
        public const string CONFIGSETTING_CRREPLACE = "CRReplace"; public const string CONFIGDEFAULT_CRREPLACE = "\\r";
        public const string CONFIGSETTING_LFREPLACE = "LFReplace"; public const string CONFIGDEFAULT_LFREPLACE = "\\n";
        public const string CONFIGSETTING_TIMESTAMPFORMAT = "TimestampFormat"; public const string CONFIGDEFAULT_TIMESTAMPFORMAT = "HH:mm:ss.fff"; // dd/MM/yyyy
        public const string CONFIGSETTING_FLUSHONWRITE = "FlushOnWrite"; public const bool CONFIGDEFAULT_FLUSHONWRITE = false;
        public const string CONFIGSETTING_WRITESTARTUPENTRIES = "WriteStartupEntries"; public const bool CONFIGDEFAULT_WRITESTARTUPENTRIES = true;
        private const string CONFIGSETTING_TRACKEXCEPTIONSASEXCEPTIONTELEMETRYENABLED = "TrackExceptionsAsExceptionTelemetryEnabled"; private const bool CONFIGDEFAULT_TRACKEXCEPTIONSASEXCEPTIONTELEMETRYENABLED = true;
        #endregion
        #region internal state
        private static Type T = typeof(TraceLoggerApplicationInsightsProvider);
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
        public TraceEventType _allowedEventTypes = TraceEventType.Critical | TraceEventType.Error | TraceEventType.Warning | TraceEventType.Information | TraceEventType.Verbose | TraceEventType.Start | TraceEventType.Stop | TraceEventType.Suspend | TraceEventType.Resume | TraceEventType.Transfer;
        TraceEntry lastWrite = default(TraceEntry);
        ILoggerProvider _provider;
        private bool _trackExceptionsAsExceptionTelemetryEnabled = true;
        private bool _trackFormattedTracesEnabled;

        public static ConcurrentDictionary<string, object> Properties { get; set; } = new ConcurrentDictionary<string, object>();
        public IList<ILogger> Listeners { get; } = new List<ILogger>();
        #endregion

        public TraceLoggerApplicationInsightsProvider() { }
        public TraceLoggerApplicationInsightsProvider(IConfiguration configuration)
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

                _CRReplace = ConfigurationHelper.GetClassSetting<TraceLoggerJsonProvider, string>(CONFIGSETTING_CRREPLACE, CONFIGDEFAULT_CRREPLACE, CultureInfo.InvariantCulture, this.ConfigurationSuffix);
                _LFReplace = ConfigurationHelper.GetClassSetting<TraceLoggerJsonProvider, string>(CONFIGSETTING_LFREPLACE, CONFIGDEFAULT_LFREPLACE, CultureInfo.InvariantCulture, this.ConfigurationSuffix);  // ConfigurationHelper.GetSetting<int>(CONFIGSETTING_LFREPLACE, CONFIGDEFAULT_LFREPLACE);
                _timestampFormat = ConfigurationHelper.GetClassSetting<TraceLoggerJsonProvider, string>(CONFIGSETTING_TIMESTAMPFORMAT, CONFIGDEFAULT_TIMESTAMPFORMAT, CultureInfo.InvariantCulture, this.ConfigurationSuffix);  // ConfigurationHelper.GetSetting<int>(CONFIGSETTING_TIMESTAMPFORMAT, CONFIGDEFAULT_TIMESTAMPFORMAT);

                _writeStartupEntries = ConfigurationHelper.GetClassSetting<TraceLoggerFormatProvider, bool>(CONFIGSETTING_WRITESTARTUPENTRIES, CONFIGDEFAULT_WRITESTARTUPENTRIES, CultureInfo.InvariantCulture, this.ConfigurationSuffix);

                _trackExceptionsAsExceptionTelemetryEnabled = ConfigurationHelper.GetClassSetting<TraceLoggerApplicationInsightsProvider, bool>(CONFIGSETTING_TRACKEXCEPTIONSASEXCEPTIONTELEMETRYENABLED, CONFIGDEFAULT_TRACKEXCEPTIONSASEXCEPTIONTELEMETRYENABLED);

                var thicksPerMillisecond = TraceLogger.Stopwatch.ElapsedTicks / TraceLogger.Stopwatch.ElapsedMilliseconds;
                string fileName = null, workingDirectory = null;
                try { fileName = TraceLogger.CurrentProcess?.StartInfo?.FileName; } catch { };
                try { workingDirectory = TraceLogger.CurrentProcess.StartInfo.WorkingDirectory; } catch { };

                scope.LogInformation($"Starting TraceLoggerApplicationInsightsProvider for: ProcessName: '{TraceLogger.ProcessName}', ProcessId: '{TraceLogger.ProcessId}', FileName: '{fileName}', WorkingDirectory: '{workingDirectory}', EntryAssemblyFullName: '{TraceLogger.EntryAssembly?.FullName}', ImageRuntimeVersion: '{TraceLogger.EntryAssembly?.ImageRuntimeVersion}', Location: '{TraceLogger.EntryAssembly?.Location}', thicksPerMillisecond: '{thicksPerMillisecond}'{Environment.NewLine}"); // "init"
                _provider = provider;
            }
        }
        public ILogger CreateLogger(string categoryName)
        {
            var innerLogger = _provider.CreateLogger(categoryName);
            var logger = new TraceLogger(this, categoryName) { TrackRawExceptions = _trackExceptionsAsExceptionTelemetryEnabled };
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
        private static TraceEntrySurrogate GetTraceSurrogate(TraceEntry entry)
        {
            var codeSection = entry.CodeSectionBase;
            return new TraceEntrySurrogate()
            {
                TraceEventType = entry.TraceEventType,
                TraceSourceName = entry.TraceSource?.Name,
                LogLevel = entry.LogLevel,
                Message = entry.Message,
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
                    Payload = codeSection.Payload,
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
}
