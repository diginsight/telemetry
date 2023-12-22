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
    //[ConfigurationName("TraceLoggerFormat")]
    public class TraceLoggerFormatProvider : ILoggerProvider, IFormatTraceEntry
    {
        #region const
        public const string CONFIGSETTING_CRREPLACE = "CRReplace"; public const string CONFIGDEFAULT_CRREPLACE = "\\r";
        public const string CONFIGSETTING_LFREPLACE = "LFReplace"; public const string CONFIGDEFAULT_LFREPLACE = "\\n";
        public const string CONFIGSETTING_TIMESTAMPFORMAT = "TimestampFormat"; public const string CONFIGDEFAULT_TIMESTAMPFORMAT = "HH:mm:ss.fff"; // dd/MM/yyyy
        public const string CONFIGSETTING_FLUSHONWRITE = "FlushOnWrite"; public const bool CONFIGDEFAULT_FLUSHONWRITE = false;
        public const string CONFIGSETTING_SHOWNESTEDFLOW = "ShowNestedFlow"; public const bool CONFIGDEFAULT_SHOWNESTEDFLOW = false;
        public const string CONFIGSETTING_SHOWTRACECOST = "ShowTraceCost"; public const bool CONFIGDEFAULT_SHOWTRACECOST = false;
        public const string CONFIGSETTING_MAXMESSAGELEVEL = "MaxMessageLevel"; public const int CONFIGDEFAULT_MAXMESSAGELEVEL = 3;
        public const string CONFIGSETTING_MAXMESSAGELEN = "MaxMessageLen"; public const int CONFIGDEFAULT_MAXMESSAGELEN = 256;
        public const string CONFIGSETTING_MAXMESSAGELENINFO = "MaxMessageLenInfo"; public const int CONFIGDEFAULT_MAXMESSAGELENINFO = 512;
        public const string CONFIGSETTING_MAXMESSAGELENWARNING = "MaxMessageLenWarning"; public const int CONFIGDEFAULT_MAXMESSAGELENWARNING = 1024;
        public const string CONFIGSETTING_MAXMESSAGELENERROR = "MaxMessageLenError"; public const int CONFIGDEFAULT_MAXMESSAGELENERROR = -1;

        public const string CONFIGSETTING_PROCESSNAMEPADDING = "ProcessNamePadding"; public const int CONFIGDEFAULT_PROCESSNAMEPADDING = 15;
        public const string CONFIGSETTING_PROCESSNAMEMAXLEN = "ProcessNameMaxLen"; public const int CONFIGDEFAULT_PROCESSNAMEMAXLEN = 30;

        public const string CONFIGSETTING_SOURCEPADDING = "SourcePadding"; public const int CONFIGDEFAULT_SOURCEPADDING = 38;
        public const string CONFIGSETTING_SOURCEMAXLEN = "SourceMaxLen"; public const int CONFIGDEFAULT_SOURCEMAXLEN = 60;
        public const string CONFIGSETTING_CATEGORYPADDING = "CategoryPadding"; public const int CONFIGDEFAULT_CATEGORYPADDING = 45;
        public const string CONFIGSETTING_CATEGORYMAXLEN = "CategoryMaxLen"; public const int CONFIGDEFAULT_CATEGORYMAXLEN = 50;
        public const string CONFIGSETTING_SOURCELEVELPADDING = "SourceLevelPadding"; public const int CONFIGDEFAULT_SOURCELEVELPADDING = 11;
        public const string CONFIGSETTING_SOURCELEVELMAXLEN = "SourceLevelMaxLen"; public const int CONFIGDEFAULT_SOURCELEVELMAXLEN = 20;
        public const string CONFIGSETTING_LOGLEVELPADDING = "LogLevelPadding"; public const int CONFIGDEFAULT_LOGLEVELPADDING = 11;
        public const string CONFIGSETTING_DELTAPADDING = "DeltaPadding"; public const int CONFIGDEFAULT_DELTAPADDING = 5;
        public const string CONFIGSETTING_LASTWRITECONTINUATIONENABLED = "LastWriteContinuationEnabled"; public const bool CONFIGDEFAULT_LASTWRITECONTINUATIONENABLED = false;
        public const string CONFIGSETTING_WRITESTARTUPENTRIES = "WriteStartupEntries"; public const bool CONFIGDEFAULT_WRITESTARTUPENTRIES = true;

        public const string CONFIGSETTING_TRACEMESSAGEFORMATPREFIX = "TraceMessageFormatPrefix"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATPREFIX = "[{now}] {source} {category} {tidpid} {operationIdPadded} - {logLevel} - {lastLineDeltaPadded} {deltaPadded} {nesting} {messageNesting}";
        public const string CONFIGSETTING_TRACEMESSAGEFORMAT = "TraceMessageFormat"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMAT = "[{now}] {source} {category} {tidpid} {operationIdPadded} - {logLevel} - {lastLineDeltaPadded} {deltaPadded} {nesting} {messageNesting}{message}";
        public const string CONFIGSETTING_TRACEMESSAGEFORMATVERBOSE = "TraceMessageFormatVerbose"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATVERBOSE = null;
        public const string CONFIGSETTING_TRACEMESSAGEFORMATINFORMATION = "TraceMessageFormatInformation"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATINFORMATION = null;
        public const string CONFIGSETTING_TRACEMESSAGEFORMATWARNING = "TraceMessageFormatWarning"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATWARNING = null;
        public const string CONFIGSETTING_TRACEMESSAGEFORMATERROR = "TraceMessageFormatError"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATERROR = null;
        public const string CONFIGSETTING_TRACEMESSAGEFORMATCRITICAL = "TraceMessageFormatCritical"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATCRITICAL = null;
        public const string CONFIGSETTING_TRACEMESSAGEFORMATSTART = "TraceMessageFormatStart"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATSTART = "[{now}] {source} {category} {tidpid} {operationIdPadded} - {logLevel} - {lastLineDeltaPadded} {deltaPadded} {nesting} {messageNesting}{message}";
        public const string CONFIGSETTING_TRACEMESSAGEFORMATSTOP = "TraceMessageFormatStop"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATSTOP = "[{now}] {source} {category} {tidpid} {operationIdPadded} - {logLevel} - {lastLineDeltaPadded} {deltaPadded} {nesting} {messageNesting}{message}{result}";
        public const string CONFIGSETTING_TRACEMESSAGEFORMATINLINESTOP = "TraceMessageFormatInlineStop"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATINLINESTOP = "... END ({delta} secs){result}";
        public const string CONFIGSETTING_TRACEMESSAGEFORMATSUSPEND = "TraceMessageFormatSuspend"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATSUSPEND = null;
        public const string CONFIGSETTING_TRACEMESSAGEFORMATRESUME = "TraceMessageFormatResume"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATRESUME = null;
        public const string CONFIGSETTING_TRACEMESSAGEFORMATTRANSFER = "TraceMessageFormatTransfer"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATTRANSFER = null;
        //"OverrideOperationIds"
        #endregion
        #region internal state
        private static Type T = typeof(TraceLoggerFormatProvider);
        private ILogger<TraceLoggerFormatProvider> logger;
        private static ClassConfigurationGetter<TraceLoggerFormatProvider> classConfigurationGetter;
        private static readonly string _traceSourceName = "TraceSource";
        public static Func<string, string> CRLF2Space = (string s) => { return s?.Replace("\r", " ")?.Replace("\n", " "); };
        public static Func<string, string> CRLF2Encode = (string s) => { return s?.Replace("\r", "\\r")?.Replace("\n", "\\n"); };
        public string Name { get; set; }
        public string ConfigurationSuffix { get; set; }
        bool _lastWriteContinuationEnabled;
        public string _CRReplace, _LFReplace;
        public string _timestampFormat;
        public bool _showNestedFlow, _showTraceCost, _flushOnWrite;
        public int _processNamePadding, _processNameMaxlen, _sourcePadding, _sourceMaxlen, _categoryPadding, _categoryMaxlen, _sourceLevelPadding, _sourceLevelMaxlen, _logLevelPadding, _deltaPadding, _traceDeltaPadding, _traceMessageFormatPrefixLen;
        public string _traceMessageFormatPrefix, _traceMessageFormat, _traceMessageFormatVerbose, _traceMessageFormatInformation, _traceMessageFormatWarning, _traceMessageFormatError, _traceMessageFormatCritical;
        public string _traceMessageFormatStart, _traceMessageFormatStop, _traceMessageFormatInlineStop, _traceMessageFormatSuspend, _traceMessageFormatResume, _traceMessageFormatTransfer;
        public string _traceDeltaDefault;
        public bool _writeStartupEntries;
        public TraceEventType _allowedEventTypes = TraceEventType.Critical | TraceEventType.Error | TraceEventType.Warning | TraceEventType.Information | TraceEventType.Verbose | TraceEventType.Start | TraceEventType.Stop | TraceEventType.Suspend | TraceEventType.Resume | TraceEventType.Transfer;
        TraceEntry lastWrite = default(TraceEntry);
        ILoggerProvider _provider;

        public static ConcurrentDictionary<string, object> Properties { get; set; } = new ConcurrentDictionary<string, object>();
        public IList<ILogger> Listeners { get; } = new List<ILogger>();
        #endregion

        #region .ctor
        public TraceLoggerFormatProvider() { }
        public TraceLoggerFormatProvider(IConfiguration configuration)
        {
            TraceLogger.InitConfiguration(configuration);
        }
        #endregion

        public void AddProvider(ILoggerProvider provider)
        {
            using (var scope = logger.BeginMethodScope(new { ConfigurationSuffix }))
            {
                if (string.IsNullOrEmpty(ConfigurationSuffix))
                {
                    var providerType = this.GetType().Name != typeof(TraceLoggerFormatProvider).Name ? this.GetType() : provider?.GetType();
                    //var prefix = providerType?.Name?.Split('.')?.Last();
                    var prefix = ConfigurationHelper.GetConfigName(providerType);
                    this.ConfigurationSuffix = prefix;
                }

                Initialize();
                _provider = provider;
            }
        }

        public void Initialize()
        {
            using (var scope = logger.BeginMethodScope())
            {
                if (classConfigurationGetter == null) { classConfigurationGetter = new ClassConfigurationGetter<TraceLoggerFormatProvider>(TraceLogger.Configuration); }

                _CRReplace = classConfigurationGetter.Get(CONFIGSETTING_CRREPLACE, CONFIGDEFAULT_CRREPLACE);
                _LFReplace = classConfigurationGetter.Get(CONFIGSETTING_LFREPLACE, CONFIGDEFAULT_LFREPLACE);
                _timestampFormat = classConfigurationGetter.Get(CONFIGSETTING_TIMESTAMPFORMAT, CONFIGDEFAULT_TIMESTAMPFORMAT);
                _showNestedFlow = classConfigurationGetter.Get(CONFIGSETTING_SHOWNESTEDFLOW, CONFIGDEFAULT_SHOWNESTEDFLOW);
                _showTraceCost = classConfigurationGetter.Get(CONFIGSETTING_SHOWTRACECOST, CONFIGDEFAULT_SHOWTRACECOST);
                _flushOnWrite = classConfigurationGetter.Get(CONFIGSETTING_FLUSHONWRITE, CONFIGDEFAULT_FLUSHONWRITE);
                _processNamePadding = classConfigurationGetter.Get(CONFIGSETTING_PROCESSNAMEPADDING, CONFIGDEFAULT_PROCESSNAMEPADDING);
                _processNameMaxlen = classConfigurationGetter.Get(CONFIGSETTING_PROCESSNAMEMAXLEN, CONFIGDEFAULT_PROCESSNAMEMAXLEN);
                _sourcePadding = classConfigurationGetter.Get(CONFIGSETTING_SOURCEPADDING, CONFIGDEFAULT_SOURCEPADDING);
                _sourceMaxlen = classConfigurationGetter.Get(CONFIGSETTING_SOURCEMAXLEN, CONFIGDEFAULT_SOURCEMAXLEN);
                _categoryPadding = classConfigurationGetter.Get(CONFIGSETTING_CATEGORYPADDING, CONFIGDEFAULT_CATEGORYPADDING);
                _categoryMaxlen = classConfigurationGetter.Get(CONFIGSETTING_CATEGORYMAXLEN, CONFIGDEFAULT_CATEGORYMAXLEN);
                _sourceLevelPadding = classConfigurationGetter.Get(CONFIGSETTING_SOURCELEVELPADDING, CONFIGDEFAULT_SOURCELEVELPADDING);
                _sourceLevelMaxlen = classConfigurationGetter.Get(CONFIGSETTING_SOURCELEVELMAXLEN, CONFIGDEFAULT_SOURCELEVELMAXLEN);
                _logLevelPadding = classConfigurationGetter.Get(CONFIGSETTING_LOGLEVELPADDING, CONFIGDEFAULT_LOGLEVELPADDING);
                _deltaPadding = classConfigurationGetter.Get(CONFIGSETTING_DELTAPADDING, CONFIGDEFAULT_DELTAPADDING);
                _lastWriteContinuationEnabled = classConfigurationGetter.Get(CONFIGSETTING_LASTWRITECONTINUATIONENABLED, CONFIGDEFAULT_LASTWRITECONTINUATIONENABLED);
                _traceMessageFormatPrefix = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATPREFIX, CONFIGDEFAULT_TRACEMESSAGEFORMATPREFIX);
                _traceMessageFormat = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMAT, CONFIGDEFAULT_TRACEMESSAGEFORMAT);
                _traceMessageFormatVerbose = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATVERBOSE, CONFIGDEFAULT_TRACEMESSAGEFORMATVERBOSE); if (string.IsNullOrEmpty(_traceMessageFormatVerbose)) { _traceMessageFormatVerbose = _traceMessageFormat; }
                _traceMessageFormatInformation = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATINFORMATION, CONFIGDEFAULT_TRACEMESSAGEFORMATINFORMATION); if (string.IsNullOrEmpty(_traceMessageFormatInformation)) { _traceMessageFormatInformation = _traceMessageFormat; }
                _traceMessageFormatWarning = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATWARNING, CONFIGDEFAULT_TRACEMESSAGEFORMATWARNING); if (string.IsNullOrEmpty(_traceMessageFormatWarning)) { _traceMessageFormatWarning = _traceMessageFormat; }
                _traceMessageFormatError = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATERROR, CONFIGDEFAULT_TRACEMESSAGEFORMATERROR); if (string.IsNullOrEmpty(_traceMessageFormatError)) { _traceMessageFormatError = _traceMessageFormat; }
                _traceMessageFormatCritical = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATCRITICAL, CONFIGDEFAULT_TRACEMESSAGEFORMATCRITICAL); if (string.IsNullOrEmpty(_traceMessageFormatCritical)) { _traceMessageFormatCritical = _traceMessageFormat; }
                _traceMessageFormatStart = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATSTART, CONFIGDEFAULT_TRACEMESSAGEFORMATSTART); if (string.IsNullOrEmpty(_traceMessageFormatStart)) { _traceMessageFormatStart = _traceMessageFormat; }
                _traceMessageFormatStop = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATSTOP, CONFIGDEFAULT_TRACEMESSAGEFORMATSTOP); if (string.IsNullOrEmpty(_traceMessageFormatStop)) { _traceMessageFormatStop = _traceMessageFormat; }
                _traceMessageFormatInlineStop = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATINLINESTOP, CONFIGDEFAULT_TRACEMESSAGEFORMATINLINESTOP);
                _traceMessageFormatSuspend = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATSUSPEND, CONFIGDEFAULT_TRACEMESSAGEFORMATSUSPEND); if (string.IsNullOrEmpty(_traceMessageFormatSuspend)) { _traceMessageFormatSuspend = _traceMessageFormat; }
                _traceMessageFormatResume = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATRESUME, CONFIGDEFAULT_TRACEMESSAGEFORMATRESUME); if (string.IsNullOrEmpty(_traceMessageFormatResume)) { _traceMessageFormatResume = _traceMessageFormat; }
                _traceMessageFormatTransfer = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATTRANSFER, CONFIGDEFAULT_TRACEMESSAGEFORMATTRANSFER); if (string.IsNullOrEmpty(_traceMessageFormatTransfer)) { _traceMessageFormatTransfer = _traceMessageFormat; }
                _writeStartupEntries = classConfigurationGetter.Get(CONFIGSETTING_WRITESTARTUPENTRIES, CONFIGDEFAULT_WRITESTARTUPENTRIES);

                var thicksPerMillisecond = TraceLogger.Stopwatch.ElapsedTicks / TraceLogger.Stopwatch.ElapsedMilliseconds;
                string fileName = null, workingDirectory = null;
                try { fileName = TraceLogger.CurrentProcess?.MainModule?.FileName; } catch { };
                try { workingDirectory = Directory.GetCurrentDirectory(); } catch { };

                scope.LogInformation($"Starting {this.GetType().Name} for: ProcessName: '{TraceLogger.ProcessName}', ProcessId: '{TraceLogger.ProcessId}', FileName: '{fileName}', WorkingDirectory: '{workingDirectory}', EntryAssemblyFullName: '{TraceLogger.EntryAssembly?.FullName}', ImageRuntimeVersion: '{TraceLogger.EntryAssembly?.ImageRuntimeVersion}', Location: '{TraceLogger.EntryAssembly?.Location}', thicksPerMillisecond: '{thicksPerMillisecond}'{Environment.NewLine}", null, new Dictionary<string, object>() { { "MaxMessageLen", 0 } }); // "init"

                scope.LogDebug($"_allowedEventTypes '{_allowedEventTypes}', _showNestedFlow '{_showNestedFlow}', _flushOnWrite '{_flushOnWrite}', _cRReplace '{_CRReplace}', _lFReplace '{_LFReplace}', _timestampFormat '{_timestampFormat}'{Environment.NewLine}", null, new Dictionary<string, object>() { { "MaxMessageLen", 0 } }); // "init" _filter '{_filter}', _categoryFilter '{_categoryFilter}',
                scope.LogDebug($"_processNamePadding '{_processNamePadding}', _sourcePadding '{_sourcePadding}', _categoryPadding '{_categoryPadding}', _sourceLevelPadding '{_sourceLevelPadding}, _logLevelPadding '{_logLevelPadding}'{Environment.NewLine}", null, new Dictionary<string, object>() { { "MaxMessageLen", 0 } }); // "init"
                scope.LogDebug($"_traceMessageFormat '{_traceMessageFormat}', _traceMessageFormatVerbose '{_traceMessageFormatVerbose}', _traceMessageFormatWarning '{_traceMessageFormatWarning}', _traceMessageFormatError '{_traceMessageFormatError}', _traceMessageFormatCritical '{_traceMessageFormatCritical}', _traceMessageFormatStart '{_traceMessageFormatStart}', _traceMessageFormatStop '{_traceMessageFormatStop}, _traceMessageFormatInlineStop '{_traceMessageFormatInlineStop}'{Environment.NewLine}", null, new Dictionary<string, object>() { { "MaxMessageLen", 0 } }); // "init"

                if (!string.IsNullOrEmpty(_traceMessageFormatPrefix))
                {
                    _traceMessageFormat = _traceMessageFormat.Substring(_traceMessageFormatPrefix.Length);
                    _traceMessageFormatVerbose = _traceMessageFormatVerbose.Substring(_traceMessageFormatPrefix.Length);
                    _traceMessageFormatInformation = _traceMessageFormatInformation.Substring(_traceMessageFormatPrefix.Length);
                    _traceMessageFormatWarning = _traceMessageFormatWarning.Substring(_traceMessageFormatPrefix.Length);
                    _traceMessageFormatError = _traceMessageFormatError.Substring(_traceMessageFormatPrefix.Length);
                    _traceMessageFormatCritical = _traceMessageFormatCritical.Substring(_traceMessageFormatPrefix.Length);

                    _traceMessageFormatStart = _traceMessageFormatStart.Substring(_traceMessageFormatPrefix.Length);
                    _traceMessageFormatStop = _traceMessageFormatStop.Substring(_traceMessageFormatPrefix.Length);
                    //_traceMessageFormatInlineStop = _traceMessageFormatInlineStop.Substring(_traceMessageFormatPrefix.Length);
                    _traceMessageFormatSuspend = _traceMessageFormatSuspend.Substring(_traceMessageFormatPrefix.Length);
                    _traceMessageFormatResume = _traceMessageFormatResume.Substring(_traceMessageFormatPrefix.Length);
                    _traceMessageFormatTransfer = _traceMessageFormatTransfer.Substring(_traceMessageFormatPrefix.Length);
                }

                int i = 0;
                var variables = "now, processName, source, category, tidpid, sourceLevel, logLevel, nesting, message, lastLineDelta, lastLineDeltaPadded, delta, deltaPadded, result, messageNesting, operationId, operationIdPadded".Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).Select((s) => new { name = s, position = i++ }).ToList();
                variables.ForEach(v =>
                {
                    _traceMessageFormatPrefix = _traceMessageFormatPrefix.Replace($"{{{v.name}}}", $"{{{v.position}}}");
                    _traceMessageFormat = _traceMessageFormat.Replace($"{{{v.name}}}", $"{{{v.position}}}");
                    _traceMessageFormatVerbose = _traceMessageFormatVerbose.Replace($"{{{v.name}}}", $"{{{v.position}}}");
                    _traceMessageFormatInformation = _traceMessageFormatInformation.Replace($"{{{v.name}}}", $"{{{v.position}}}");
                    _traceMessageFormatWarning = _traceMessageFormatWarning.Replace($"{{{v.name}}}", $"{{{v.position}}}");
                    _traceMessageFormatError = _traceMessageFormatError.Replace($"{{{v.name}}}", $"{{{v.position}}}");
                    _traceMessageFormatCritical = _traceMessageFormatCritical.Replace($"{{{v.name}}}", $"{{{v.position}}}");

                    _traceMessageFormatStart = _traceMessageFormatStart.Replace($"{{{v.name}}}", $"{{{v.position}}}");
                    _traceMessageFormatStop = _traceMessageFormatStop.Replace($"{{{v.name}}}", $"{{{v.position}}}");
                    _traceMessageFormatInlineStop = _traceMessageFormatInlineStop.Replace($"{{{v.name}}}", $"{{{v.position}}}");
                    _traceMessageFormatSuspend = _traceMessageFormatSuspend.Replace($"{{{v.name}}}", $"{{{v.position}}}");
                    _traceMessageFormatResume = _traceMessageFormatResume.Replace($"{{{v.name}}}", $"{{{v.position}}}");
                    _traceMessageFormatTransfer = _traceMessageFormatTransfer.Replace($"{{{v.name}}}", $"{{{v.position}}}");
                });

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

        #region GetMaxMessageLen
        public static int? GetMaxMessageLen(ICodeSection section, TraceEventType traceEventType)
        {
            var maxMessageLenSpecific = default(int?);
            switch (traceEventType)
            {
                case TraceEventType.Error:
                case TraceEventType.Critical:
                    var maxMessageLenError = section?._maxMessageLenError ?? section?.ModuleContext?.MaxMessageLenError;
                    if (maxMessageLenError == null)
                    {
                        //var val = ConfigurationHelper.GetSetting("MaxMessageLenError", TraceLoggerFormatProvider.CONFIGDEFAULT_MAXMESSAGELENERROR);
                        var val = classConfigurationGetter.Get("MaxMessageLenError", TraceLoggerFormatProvider.CONFIGDEFAULT_MAXMESSAGELENERROR);
                        if (val != 0) { maxMessageLenError = val; if (section != null) { section._maxMessageLenError = maxMessageLenError; if (section.ModuleContext != null) { section.ModuleContext.MaxMessageLenError = maxMessageLenError; } } }
                    }
                    if (maxMessageLenError != 0) { maxMessageLenSpecific = maxMessageLenError; }
                    break;
                case TraceEventType.Warning:
                    var maxMessageLenWarning = section?._maxMessageLenWarning ?? section?.ModuleContext?.MaxMessageLenWarning;
                    if (maxMessageLenWarning == null)
                    {
                        //var val = ConfigurationHelper.GetSetting<int>("MaxMessageLenWarning", TraceLoggerFormatProvider.CONFIGDEFAULT_MAXMESSAGELENWARNING);
                        var val = classConfigurationGetter.Get("MaxMessageLenWarning", TraceLoggerFormatProvider.CONFIGDEFAULT_MAXMESSAGELENWARNING);
                        if (val != 0) { maxMessageLenWarning = val; if (section != null) { section._maxMessageLenWarning = maxMessageLenWarning; if (section.ModuleContext != null) { section.ModuleContext.MaxMessageLenWarning = maxMessageLenWarning; } } }
                    }
                    if (maxMessageLenWarning != 0) { maxMessageLenSpecific = maxMessageLenWarning; }
                    break;
                case TraceEventType.Information:
                    var maxMessageLenInfo = section?._maxMessageLenInfo ?? section?.ModuleContext?.MaxMessageLenInfo;
                    if (maxMessageLenInfo == null)
                    {
                        //var val = ConfigurationHelper.GetSetting<int>("MaxMessageLenInfo", TraceLoggerFormatProvider.CONFIGDEFAULT_MAXMESSAGELENINFO);
                        var val = classConfigurationGetter.Get("MaxMessageLenInfo", TraceLoggerFormatProvider.CONFIGDEFAULT_MAXMESSAGELENINFO);
                        if (val != 0) { maxMessageLenInfo = val; if (section != null) { section._maxMessageLenInfo = maxMessageLenInfo; if (section.ModuleContext != null) { section.ModuleContext.MaxMessageLenInfo = maxMessageLenInfo; } } }
                    }
                    if (maxMessageLenInfo != 0) { maxMessageLenSpecific = maxMessageLenInfo; }
                    break;
                case TraceEventType.Verbose:
                    var maxMessageLenVerbose = section?._maxMessageLenVerbose ?? section?.ModuleContext?.MaxMessageLenVerbose;
                    if (maxMessageLenVerbose == null)
                    {
                        //var val = ConfigurationHelper.GetSetting<int>("MaxMessageLenVerbose", TraceLoggerFormatProvider.CONFIGDEFAULT_MAXMESSAGELENINFO);
                        var val = classConfigurationGetter.Get("MaxMessageLenVerbose", TraceLoggerFormatProvider.CONFIGDEFAULT_MAXMESSAGELENINFO);
                        if (val != 0) { maxMessageLenVerbose = val; if (section != null) { section._maxMessageLenVerbose = maxMessageLenVerbose; if (section.ModuleContext != null) { section.ModuleContext.MaxMessageLenVerbose = maxMessageLenVerbose; } } }
                    }
                    if (maxMessageLenVerbose != 0) { maxMessageLenSpecific = maxMessageLenVerbose; }
                    break;
            }
            var maxMessageLen = maxMessageLenSpecific ?? section?._maxMessageLen ?? section?.ModuleContext?.MaxMessageLen;
            if (maxMessageLen == null)
            {
                //maxMessageLen = ConfigurationHelper.GetSetting<int>("MaxMessageLen", TraceLoggerFormatProvider.CONFIGDEFAULT_MAXMESSAGELEN);
                maxMessageLen = classConfigurationGetter.Get("MaxMessageLen", TraceLoggerFormatProvider.CONFIGDEFAULT_MAXMESSAGELEN);
                if (section != null) { section._maxMessageLen = maxMessageLen; if (section.ModuleContext != null) { section.ModuleContext.MaxMessageLen = maxMessageLen; } }
            }
            if (section != null) { section._maxMessageLen = maxMessageLen; }
            return maxMessageLen;
        }
        #endregion
        public string FormatTraceEntry(TraceEntry entry, Exception ex)
        {
            var message = null as string;
            var isLastWriteContinuation = false;

            message = getEntryMessage(entry, lastWrite, out isLastWriteContinuation);
            message += Environment.NewLine;

            // check the global filter
            //if (isLastWriteContinuation) { sbMessages.Remove(sbMessages.Length - 2, 2); }
            lastWrite = entry;
            if (entry.Equals(default(TraceEntry))) { lastWrite.ElapsedMilliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds; }

            var traceDeltaPadded = default(string);

            var endTraceTicks = TraceLogger.Stopwatch.ElapsedTicks;
            traceDeltaPadded = (endTraceTicks - entry.TraceStartTicks).ToString("###0"); // .PadLeft(5)
            if (traceDeltaPadded != null && traceDeltaPadded.Length < _traceDeltaPadding) { traceDeltaPadded = traceDeltaPadded.PadLeft(_traceDeltaPadding); }
            if (traceDeltaPadded.Length > _traceDeltaPadding) { _traceDeltaPadding = traceDeltaPadded.Length; _traceDeltaDefault = new string(' ', _traceDeltaPadding); }

            var fullMessage = _showTraceCost ? $"{traceDeltaPadded}.{message}" : message;
            return fullMessage;
        }
        public string getEntryMessage(TraceEntry entry, TraceEntry lastWrite, out bool isLastWriteContinuation)
        {
            isLastWriteContinuation = false;

            // {placeholder} placeholderTruncate PadRightExact
            var line = default(string);
            var category = entry.Category ?? "general";
            var processName = TraceLogger.ProcessName + ".exe";
            var source = entry.Source ?? "unknown";

            var codeSection = entry.CodeSectionBase;
            if (processName != null && processName.Length < _processNamePadding) { processName = processName.PadRight(_processNamePadding); }
            if (source != null && source.Length < _sourcePadding) { source = source.PadRight(_sourcePadding); }
            if (source.Length > _sourcePadding) { _sourcePadding = Min(source.Length, _sourceMaxlen); source = source.PadRightExact(_sourcePadding); }
            if (category != null && category.Length < _categoryPadding) { category = category.PadRight(_categoryPadding); }
            if (category.Length > _categoryPadding) { _categoryPadding = Min(category.Length, _categoryMaxlen); category = category.PadRightExact(_categoryPadding); }

            var sourceLevel = entry.SourceLevel.ToString();
            if (sourceLevel != null && sourceLevel.Length < _sourceLevelPadding) { sourceLevel = sourceLevel.PadRight(_sourceLevelPadding); }
            if (sourceLevel.Length > _sourceLevelPadding) { _sourceLevelPadding = sourceLevel.Length; }

            var logLevel = entry.LogLevel.ToString();
            if (logLevel != null && logLevel.Length < _logLevelPadding) { logLevel = logLevel.PadRight(_logLevelPadding); }
            if (logLevel.Length > _logLevelPadding) { _logLevelPadding = logLevel.Length; }

            var tidpid = TraceLogger.ProcessId > 0 ? $"{TraceLogger.ProcessId,5} {entry.ThreadID,4}" : $"{entry.ThreadID,4}";
            if (entry.ApartmentState != ApartmentState.Unknown) { tidpid += $" {entry.ApartmentState}"; }

            var maxMessageLen = default(int?);
            if (entry.Properties != null && entry.Properties.TryGetValue("MaxMessageLen", out var maxMessageLenObject))
            {
                maxMessageLen = (int?)maxMessageLenObject;
            }
            else
            {
                maxMessageLen = TraceLoggerFormatProvider.GetMaxMessageLen(codeSection, entry.TraceEventType);
            }

            var messageRaw = entry.Message;
            if (entry.GetMessage != null) { messageRaw = entry.GetMessage(); }
            else if (entry.MessageFormat != null) { messageRaw = string.Format(entry.MessageFormat, entry.MessageArgs); }
            else if (entry.MessageObject != null) { messageRaw = entry.MessageObject.GetLogString(); }

            var message = codeSection.IsInnerScope ? "... " + messageRaw : messageRaw;
            if (maxMessageLen > 0 && message != null && message.Length > maxMessageLen) { message = message.Substring(0, maxMessageLen.Value - 3) + "..."; }

            var nesting = getNesting(entry);
            //string operationID = !string.IsNullOrEmpty(entry.RequestContext?.RequestId) ? entry.RequestContext?.RequestId : "null";
            string operationID = entry.CodeSectionBase?.OperationID;
            string operationIdPadded = operationID.PadRightExact(12, null, false);
            var now = DateTime.Now.ToString(_timestampFormat);

            var delta = ""; var lastLineDelta = ""; var lastLineDeltaSB = new StringBuilder();
            var deltaPadded = ""; var lastLineDeltaPadded = "";
            var resultString = "";
            var messageNesting = _showNestedFlow ? new string(' ', entry.CodeSectionBase != null ? entry.CodeSectionBase.NestingLevel * 2 : 0) : "";

            lastLineDeltaPadded = lastLineDelta = getLastLineDeltaOptimized(entry, lastWrite); // .PadLeft(5)
            if (lastLineDeltaPadded != null && lastLineDeltaPadded.Length < _deltaPadding) { lastLineDeltaPadded = lastLineDeltaPadded.PadLeft(_deltaPadding); }
            if (lastLineDeltaPadded.Length > _deltaPadding) { _deltaPadding = lastLineDeltaPadded.Length; }

            var linePrefix = ""; var lineSuffix = "";
            switch (entry.TraceEventType)
            {
                case TraceEventType.Start:
                    // line = $"[{now}] {processName} {source} {category} {tidpid} - {sourceLevel} - {nesting} {message}"; "[{0}] {1} {2} {3} - {4,-11} - {5} {6}"
                    string section = !string.IsNullOrEmpty(codeSection.Name) ? string.Format(".{0}", codeSection.Name) : null;
                    message = $"{getClassName(codeSection)}.{codeSection.MemberName}{section}() START ";
                    if (codeSection.Payload != null)
                    {
                        var payload = codeSection.Payload;
                        if (payload is Delegate)
                        {
                            try { payload = ((Delegate)codeSection.Payload).DynamicInvoke(); }
                            catch (Exception) { /*payload = ex; */}
                        }

                        var payloadString = $"{payload.GetLogString()}";
                        var maxPayloadLen = maxMessageLen >= 0 ? (int)maxMessageLen - Min((int)maxMessageLen, message.Length) : -1;
                        if (payloadString.Length > maxPayloadLen)
                        {
                            var deltaLen = maxPayloadLen > 3 ? maxPayloadLen - 3 : maxPayloadLen;
                            if (deltaLen > 0) { payloadString = payloadString.Substring(0, deltaLen) + "..."; }
                        }
                        message = $"{getClassName(codeSection)}.{codeSection.MemberName}{section}({payloadString}) START ";
                    }

                    deltaPadded = delta = ""; // .PadLeft(5)
                    if (deltaPadded != null && deltaPadded.Length < _deltaPadding) { deltaPadded = deltaPadded.PadLeft(_deltaPadding); }
                    if (deltaPadded.Length > _deltaPadding) { _deltaPadding = deltaPadded.Length; }

                    if (!string.IsNullOrEmpty(_traceMessageFormatPrefix)) { linePrefix = string.Format(_traceMessageFormatPrefix, now, processName, source, category, tidpid, sourceLevel, logLevel, nesting, message, lastLineDelta, lastLineDeltaPadded, delta, deltaPadded, resultString, messageNesting, operationID, operationIdPadded); }
                    if (!string.IsNullOrEmpty(_traceMessageFormatStart)) { lineSuffix = string.Format(_traceMessageFormatStart, now, processName, source, category, tidpid, sourceLevel, logLevel, nesting, message, lastLineDelta, lastLineDeltaPadded, delta, deltaPadded, resultString, messageNesting, operationID, operationIdPadded); }
                    line = $"{linePrefix}{lineSuffix}";
                    break;
                case TraceEventType.Stop:
                    // line = $"[{now}] {processName} {source} {category} {tidpid} - {sourceLevel} - {nesting} {message}"; "[{0}] {1} {2} {3} - {4,-11} - {5} {6} ({7} secs)"
                    section = !string.IsNullOrEmpty(codeSection.Name) ? string.Format(".{0}", codeSection.Name) : null;
                    message = string.Format("{0}.{1}{2}() END", !string.IsNullOrWhiteSpace(getClassName(codeSection)) ? getClassName(codeSection) : string.Empty, codeSection.MemberName, section);
                    if (codeSection.Result != null)
                    {
                        var maxResultLen = maxMessageLen >= 0 ? (int)maxMessageLen - Min((int)maxMessageLen, message.Length) : -1;
                        resultString = $" returned {codeSection.Result.GetLogString()}";
                        if (resultString.Length > maxResultLen)
                        {
                            var deltaLen = maxResultLen > 3 ? maxResultLen - 13 : maxResultLen;
                            if (deltaLen > 0) { resultString = resultString.Substring(0, deltaLen) + "..."; }
                        }
                    }

                    var milliseconds = TraceLogger.Stopwatch.ElapsedMilliseconds - codeSection.CallStartMilliseconds;
                    deltaPadded = delta = ((float)milliseconds / 1000).ToString("###0.00"); // .PadLeft(5)
                    if (deltaPadded != null && deltaPadded.Length < _deltaPadding) { deltaPadded = deltaPadded.PadLeft(_deltaPadding); }
                    if (deltaPadded.Length > _deltaPadding) { _deltaPadding = deltaPadded.Length; }

                    var traceMessageFormat = _traceMessageFormatStop;
                    if (_lastWriteContinuationEnabled == true && lastWrite.CodeSectionBase == codeSection && lastWrite.TraceEventType == TraceEventType.Start)
                    {
                        isLastWriteContinuation = true;
                        traceMessageFormat = _traceMessageFormatInlineStop;
                    }

                    if (!string.IsNullOrEmpty(_traceMessageFormatPrefix)) { linePrefix = string.Format(_traceMessageFormatPrefix, now, processName, source, category, tidpid, sourceLevel, logLevel, nesting, message, lastLineDelta, lastLineDeltaPadded, delta, deltaPadded, resultString, messageNesting, operationID, operationIdPadded); }
                    if (!string.IsNullOrEmpty(traceMessageFormat)) { lineSuffix = string.Format(traceMessageFormat, now, processName, source, category, tidpid, sourceLevel, logLevel, nesting, message, lastLineDelta, lastLineDeltaPadded, delta, deltaPadded, resultString, messageNesting, operationID, operationIdPadded); }
                    line = $"{linePrefix}{lineSuffix}";
                    break;
                default: // case TraceEntryType.Message:
                    // line = $"[{now}] {processName} {source} {category} {tidpid} - {sourceLevel} - {nesting}   {message}"; "[{0}] {1} {2} {3} - {4,-11} - {5}   {6}"
                    deltaPadded = delta = ""; // .PadLeft(5)
                    if (deltaPadded != null && deltaPadded.Length < _deltaPadding) { deltaPadded = deltaPadded.PadLeft(_deltaPadding); }
                    if (deltaPadded.Length > _deltaPadding) { _deltaPadding = deltaPadded.Length; }
                    if (_showNestedFlow) { messageNesting += "  "; }
                    if (!string.IsNullOrEmpty(_traceMessageFormatPrefix)) { linePrefix = string.Format(_traceMessageFormatPrefix, now, processName, source, category, tidpid, sourceLevel, logLevel, nesting, message, lastLineDelta, lastLineDeltaPadded, delta, deltaPadded, resultString, messageNesting, operationID, operationIdPadded); }
                    if (!string.IsNullOrEmpty(_traceMessageFormatInformation)) { lineSuffix = string.Format(_traceMessageFormatInformation, now, processName, source, category, tidpid, sourceLevel, logLevel, nesting, message, lastLineDelta, lastLineDeltaPadded, delta, deltaPadded, resultString, messageNesting, operationID, operationIdPadded); }
                    line = $"{linePrefix}{lineSuffix}";
                    break;
            }

            if (!entry.DisableCRLFReplace)
            {
                if (line.IndexOf('\n') >= 0 || line.IndexOf('\r') >= 0)
                {
                    if (!string.IsNullOrEmpty(_CRReplace)) { line = line?.Replace("\r", _CRReplace); }
                    if (!string.IsNullOrEmpty(_LFReplace)) { line = line?.Replace("\n", _LFReplace); }
                }
            }
            else
            {
                var prefixFill = new string(' ', linePrefix.Length);
                if (line.IndexOf('\n') >= 0) { line = line?.Replace("\n", $"\n{prefixFill}"); }
            }
            return line;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string getNesting(TraceEntry entry)
        {
            var requestInfo = entry.RequestContext;
            var dept = requestInfo != null ? requestInfo.RequestDept : 0;

            var section = entry.CodeSectionBase;
            var showNestedFlow = _showNestedFlow;
            string deptString = $"{dept}.{(section != null ? section.NestingLevel : 0)}".PadLeftExact(4, ' ');
            // if (showNestedFlow == false) { return deptString; }

            return deptString;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string getLastLineDeltaOptimized(TraceEntry entry, TraceEntry lastWrite)
        {
            var milliseconds = entry.ElapsedMilliseconds - lastWrite.ElapsedMilliseconds;
            if (milliseconds <= 0) { return ""; }

            var seconds = (float)milliseconds / 1000; // .PadLeft(5)
            int minutes = 0, hours = 0, days = 0; // months = 0, years = 0;
            string lastLineDelta = null;

            if (seconds < 0.005) { return ""; }
            if (seconds < 60) { lastLineDelta = seconds.ToString("#0.00"); return lastLineDelta; }

            minutes = ((int)seconds / 60);
            seconds = seconds % 60;
            lastLineDelta = minutes >= 60 ? $"{seconds:00.00}" : $"{seconds:#0.00}";

            var lastLineDeltaSB = new StringBuilder(lastLineDelta);
            if (minutes >= 60)
            {
                hours = ((int)seconds / 60);
                minutes = minutes % 60;
                lastLineDeltaSB.Insert(0, hours >= 24 ? $"{minutes:00}:" : $"{minutes:#0}:");
            }
            else if (minutes > 0) { lastLineDeltaSB.Insert(0, $"{minutes:#0}:"); }
            if (hours >= 24)
            {
                days = ((int)hours / 24);
                hours = hours % 24;
                lastLineDeltaSB.Insert(0, $"{hours:#0}:");
            }
            else if (hours > 0) { lastLineDeltaSB.Insert(0, $"{hours:#0}:"); }
            if (days > 0)
            {
                lastLineDeltaSB.Insert(0, $"{days:#.##0}:");
            }
            return lastLineDeltaSB.ToString();
        }
        #region Min
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int Min(int a, int b) { return a < b ? a : b; }
        #endregion

        string getClassName(ICodeSection sec)
        {
            return sec.T != null ? sec.T?.Name : sec.ClassName;
        }
    }

    [ProviderAlias("DiginsightFormattedLog4Net")]
    [ConfigurationName("DiginsightFormattedLog4Net")]
    public class DiginsightFormattedLog4NetProvider : TraceLoggerFormatProvider
    {
        #region .ctor
        public DiginsightFormattedLog4NetProvider() { }
        public DiginsightFormattedLog4NetProvider(IConfiguration configuration) : base(configuration) { }
        #endregion
    }

    [ProviderAlias("DiginsightFormattedApplicationInsights")]
    [ConfigurationName("DiginsightFormattedApplicationInsights")]
    public class DiginsightFormattedApplicationInsightsProvider : TraceLoggerFormatProvider
    {
        #region .ctor
        public DiginsightFormattedApplicationInsightsProvider() { }
        public DiginsightFormattedApplicationInsightsProvider(IConfiguration configuration) : base(configuration) { }
        #endregion
    }

    [ProviderAlias("DiginsightFormattedConsole")]
    [ConfigurationName("DiginsightFormattedConsole")]
    public class DiginsightFormattedConsoleProvider : TraceLoggerFormatProvider
    {
        #region .ctor
        public DiginsightFormattedConsoleProvider() { }
        public DiginsightFormattedConsoleProvider(IConfiguration configuration) : base(configuration)
        {
            if (_instance == null)
            {
                _instance = new DiginsightFormattedConsoleProvider();
            }
        }
        #endregion

        static DiginsightFormattedConsoleProvider _instance;
        public static DiginsightFormattedConsoleProvider Instance
        {
            get { return _instance; }
            set { _instance = value; }
        }
    }

    [ProviderAlias("DiginsightFormattedDebug")]
    [ConfigurationName("DiginsightFormattedDebug")]
    public class DiginsightFormattedDebugProvider : TraceLoggerFormatProvider
    {
        #region .ctor
        public DiginsightFormattedDebugProvider() { }
        public DiginsightFormattedDebugProvider(IConfiguration configuration) : base(configuration) { }
        #endregion

        static DiginsightFormattedConsoleProvider _instance;
        public static DiginsightFormattedConsoleProvider Instance
        {
            get { return _instance; }
            set { _instance = value; }
        }
    }
}
