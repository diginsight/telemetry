#region using
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Text;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
//using Microsoft.WindowsAzure.WebSites.Diagnostics;
#endregion

namespace Common
{
    public class TraceListenerFormatItems : TraceListener, ISupportFilters, ISupportInnerListener
    {
        #region const
        public const string FILTER_SPLIT_REGEX = @"([-!]?[\""].+?[\""])|([-!]?\w+)";
        public const string CONFIGSETTING_CRREPLACE = "CRReplace"; public const string CONFIGDEFAULT_CRREPLACE = "\\r";
        public const string CONFIGSETTING_LFREPLACE = "LFReplace"; public const string CONFIGDEFAULT_LFREPLACE = "\\n";
        public const string CONFIGSETTING_FILTER = "Filter"; public const string CONFIGDEFAULT_FILTER = null;
        public const string CONFIGSETTING_CATEGORYFILTER = "CategoryFilter"; public const string CONFIGDEFAULT_CATEGORYFILTER = null;
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
        public const string CONFIGSETTING_SOURCEPADDING = "SourcePadding"; public const int CONFIGDEFAULT_SOURCEPADDING = 5;
        public const string CONFIGSETTING_CATEGORYPADDING = "CategoryPadding"; public const int CONFIGDEFAULT_CATEGORYPADDING = 5;
        public const string CONFIGSETTING_SOURCELEVELPADDING = "SourceLevelPadding"; public const int CONFIGDEFAULT_SOURCELEVELPADDING = 11;
        public const string CONFIGSETTING_DELTAPADDING = "DeltaPadding"; public const int CONFIGDEFAULT_DELTAPADDING = 5;
        public const string CONFIGSETTING_LASTWRITECONTINUATIONENABLED = "LastWriteContinuationEnabled"; public const bool CONFIGDEFAULT_LASTWRITECONTINUATIONENABLED = false;

        public const string CONFIGSETTING_TRACEMESSAGEFORMATPREFIX = "TraceMessageFormatPrefix"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATPREFIX = "[{now}] {source} {category} {tidpid} - {sourceLevel} - {lastLineDeltaPadded} {deltaPadded} {nesting} {messageNesting}";
        public const string CONFIGSETTING_TRACEMESSAGEFORMAT = "TraceMessageFormat"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMAT = "[{now}] {source} {category} {tidpid} - {sourceLevel} - {lastLineDeltaPadded} {deltaPadded} {nesting} {messageNesting}{message}";
        public const string CONFIGSETTING_TRACEMESSAGEFORMATVERBOSE = "TraceMessageFormatVerbose"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATVERBOSE = null;
        public const string CONFIGSETTING_TRACEMESSAGEFORMATINFORMATION = "TraceMessageFormatInformation"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATINFORMATION = null;
        public const string CONFIGSETTING_TRACEMESSAGEFORMATWARNING = "TraceMessageFormatWarning"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATWARNING = null;
        public const string CONFIGSETTING_TRACEMESSAGEFORMATERROR = "TraceMessageFormatError"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATERROR = null;
        public const string CONFIGSETTING_TRACEMESSAGEFORMATCRITICAL = "TraceMessageFormatCritical"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATCRITICAL = null;
        public const string CONFIGSETTING_TRACEMESSAGEFORMATSTART = "TraceMessageFormatStart"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATSTART = "[{now}] {source} {category} {tidpid} - {sourceLevel} - {lastLineDeltaPadded} {deltaPadded} {nesting} {messageNesting}{message}";
        public const string CONFIGSETTING_TRACEMESSAGEFORMATSTOP = "TraceMessageFormatStop"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATSTOP = "[{now}] {source} {category} {tidpid} - {sourceLevel} - {lastLineDeltaPadded} {deltaPadded} {nesting} {messageNesting}{message}{result}";
        public const string CONFIGSETTING_TRACEMESSAGEFORMATINLINESTOP = "TraceMessageFormatInlineStop"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATINLINESTOP = "... END ({delta} secs){result}";
        public const string CONFIGSETTING_TRACEMESSAGEFORMATSUSPEND = "TraceMessageFormatSuspend"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATSUSPEND = null;
        public const string CONFIGSETTING_TRACEMESSAGEFORMATRESUME = "TraceMessageFormatResume"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATRESUME = null;
        public const string CONFIGSETTING_TRACEMESSAGEFORMATTRANSFER = "TraceMessageFormatTransfer"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATTRANSFER = null;
        #endregion
        #region internal state
        ILogger<TraceListenerFormatItems> logger;
        bool _lastWriteContinuationEnabled;
        public string _CRReplace, _LFReplace;
        public string _timestampFormat;
        public bool _showNestedFlow, _showTraceCost, _flushOnWrite;
        public int _processNamePadding, _sourcePadding, _categoryPadding, _sourceLevelPadding, _deltaPadding, _traceDeltaPadding, _traceMessageFormatPrefixLen;
        public string _traceMessageFormatPrefix, _traceMessageFormat, _traceMessageFormatVerbose, _traceMessageFormatInformation, _traceMessageFormatWarning, _traceMessageFormatError, _traceMessageFormatCritical;
        public string _traceMessageFormatStart, _traceMessageFormatStop, _traceMessageFormatInlineStop, _traceMessageFormatSuspend, _traceMessageFormatResume, _traceMessageFormatTransfer;
        public string _filter, _categoryFilter;
        public string _traceDeltaDefault;
        public TraceEventType _allowedEventTypes = TraceEventType.Critical | TraceEventType.Error | TraceEventType.Warning | TraceEventType.Information | TraceEventType.Verbose | TraceEventType.Start | TraceEventType.Stop | TraceEventType.Suspend | TraceEventType.Resume | TraceEventType.Transfer;
        //private readonly int _timeout = 10;
        TraceEntry lastWrite = default(TraceEntry);
        //private ILog _log;
        //static ILoggerRepository _logRepository = null;
        //#endregion
        //#region internal state
        //static Stopwatch _stopwatch = TraceManager.Stopwatch;
        //Timer _timer = null;
        //long _lastTickWriteTimestamp = 0;
        //ConcurrentQueue<object> _entries = new ConcurrentQueue<object>();
        //ConcurrentQueue<object> _nextEntries = new ConcurrentQueue<object>();
        #endregion

        #region .ctor
        static TraceListenerFormatItems()
        {
        }
        public TraceListenerFormatItems()
        {
            using (var scope = logger.BeginMethodScope())
            {
                Init();
            }
        }
        public TraceListenerFormatItems(TraceListener innerListener) : this()
        {
            using (var scope = logger.BeginMethodScope(new { innerListener }))
            {
                InnerListener = innerListener;
            }
        }
        #endregion

        private void Init()
        {
            using (var scope = logger.BeginMethodScope())
            {
                var classConfigurationGetter = new ClassConfigurationGetter<TraceListenerFormatItems>(TraceLogger.Configuration);

                //_CRReplace = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, string>(CONFIGSETTING_CRREPLACE, CONFIGDEFAULT_CRREPLACE);
                _CRReplace = classConfigurationGetter.Get(CONFIGSETTING_CRREPLACE, CONFIGDEFAULT_CRREPLACE);
                //_LFReplace = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, string>(CONFIGSETTING_LFREPLACE, CONFIGDEFAULT_LFREPLACE);  // ConfigurationHelper.GetSetting<int>(CONFIGSETTING_LFREPLACE, CONFIGDEFAULT_LFREPLACE);
                _LFReplace = classConfigurationGetter.Get(CONFIGSETTING_LFREPLACE, CONFIGDEFAULT_LFREPLACE);
                //var filter = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, string>(CONFIGSETTING_FILTER, CONFIGDEFAULT_FILTER);
                var filter = classConfigurationGetter.Get(CONFIGSETTING_FILTER, CONFIGDEFAULT_FILTER);
                if (!string.IsNullOrEmpty(filter)) { ((ISupportFilters)this).Filter = filter; }
                //var categoryFilter = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, string>(CONFIGSETTING_CATEGORYFILTER, CONFIGDEFAULT_CATEGORYFILTER);
                var categoryFilter = classConfigurationGetter.Get(CONFIGSETTING_CATEGORYFILTER, CONFIGDEFAULT_CATEGORYFILTER);
                if (!string.IsNullOrEmpty(categoryFilter)) { this.CategoryFilter = categoryFilter; }

                //_timestampFormat = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, string>(CONFIGSETTING_TIMESTAMPFORMAT, CONFIGDEFAULT_TIMESTAMPFORMAT);  // ConfigurationHelper.GetSetting<int>(CONFIGSETTING_TIMESTAMPFORMAT, CONFIGDEFAULT_TIMESTAMPFORMAT);
                _timestampFormat = classConfigurationGetter.Get(CONFIGSETTING_TIMESTAMPFORMAT, CONFIGDEFAULT_TIMESTAMPFORMAT);
                //_showNestedFlow = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, bool>(CONFIGSETTING_SHOWNESTEDFLOW, CONFIGDEFAULT_SHOWNESTEDFLOW);
                _showNestedFlow = classConfigurationGetter.Get(CONFIGSETTING_SHOWNESTEDFLOW, CONFIGDEFAULT_SHOWNESTEDFLOW);
                //_showTraceCost = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, bool>(CONFIGSETTING_SHOWTRACECOST, CONFIGDEFAULT_SHOWTRACECOST);
                _showTraceCost = classConfigurationGetter.Get(CONFIGSETTING_SHOWTRACECOST, CONFIGDEFAULT_SHOWTRACECOST);
                //_flushOnWrite = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, bool>(CONFIGSETTING_FLUSHONWRITE, CONFIGDEFAULT_FLUSHONWRITE);
                _flushOnWrite = classConfigurationGetter.Get(CONFIGSETTING_FLUSHONWRITE, CONFIGDEFAULT_FLUSHONWRITE);
                //_processNamePadding = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, int>(CONFIGSETTING_PROCESSNAMEPADDING, CONFIGDEFAULT_PROCESSNAMEPADDING);
                _processNamePadding = classConfigurationGetter.Get(CONFIGSETTING_PROCESSNAMEPADDING, CONFIGDEFAULT_PROCESSNAMEPADDING);
                //_sourcePadding = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, int>(CONFIGSETTING_SOURCEPADDING, CONFIGDEFAULT_SOURCEPADDING);
                _sourcePadding = classConfigurationGetter.Get(CONFIGSETTING_SOURCEPADDING, CONFIGDEFAULT_SOURCEPADDING);
                //_categoryPadding = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, int>(CONFIGSETTING_CATEGORYPADDING, CONFIGDEFAULT_CATEGORYPADDING);
                _categoryPadding = classConfigurationGetter.Get(CONFIGSETTING_CATEGORYPADDING, CONFIGDEFAULT_CATEGORYPADDING);
                //_sourceLevelPadding = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, int>(CONFIGSETTING_SOURCELEVELPADDING, CONFIGDEFAULT_SOURCELEVELPADDING);
                _sourceLevelPadding = classConfigurationGetter.Get(CONFIGSETTING_SOURCELEVELPADDING, CONFIGDEFAULT_SOURCELEVELPADDING);
                //_sourceLevelPadding = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, int>(CONFIGSETTING_DELTAPADDING, CONFIGDEFAULT_DELTAPADDING);
                _sourceLevelPadding = classConfigurationGetter.Get(CONFIGSETTING_DELTAPADDING, CONFIGDEFAULT_DELTAPADDING);
                //_lastWriteContinuationEnabled = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, bool>(CONFIGSETTING_LASTWRITECONTINUATIONENABLED, CONFIGDEFAULT_LASTWRITECONTINUATIONENABLED);
                //_traceMessageFormatPrefix = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, string>(CONFIGSETTING_TRACEMESSAGEFORMATPREFIX, CONFIGDEFAULT_TRACEMESSAGEFORMATPREFIX);
                _traceMessageFormatPrefix = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATPREFIX, CONFIGDEFAULT_TRACEMESSAGEFORMATPREFIX);
                //_traceMessageFormat = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, string>(CONFIGSETTING_TRACEMESSAGEFORMAT, CONFIGDEFAULT_TRACEMESSAGEFORMAT);
                _traceMessageFormat = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMAT, CONFIGDEFAULT_TRACEMESSAGEFORMAT);
                //_traceMessageFormatVerbose = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, string>(CONFIGSETTING_TRACEMESSAGEFORMATVERBOSE, CONFIGDEFAULT_TRACEMESSAGEFORMATVERBOSE); if (string.IsNullOrEmpty(_traceMessageFormatVerbose)) { _traceMessageFormatVerbose = _traceMessageFormat; }
                _traceMessageFormatVerbose = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATVERBOSE, CONFIGDEFAULT_TRACEMESSAGEFORMATVERBOSE); if (string.IsNullOrEmpty(_traceMessageFormatVerbose)) { _traceMessageFormatVerbose = _traceMessageFormat; }
                //_traceMessageFormatInformation = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, string>(CONFIGSETTING_TRACEMESSAGEFORMATINFORMATION, CONFIGDEFAULT_TRACEMESSAGEFORMATINFORMATION); if (string.IsNullOrEmpty(_traceMessageFormatInformation)) { _traceMessageFormatInformation = _traceMessageFormat; }
                _traceMessageFormatInformation = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATINFORMATION, CONFIGDEFAULT_TRACEMESSAGEFORMATINFORMATION); if (string.IsNullOrEmpty(_traceMessageFormatInformation)) { _traceMessageFormatInformation = _traceMessageFormat; }
                //_traceMessageFormatWarning = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, string>(CONFIGSETTING_TRACEMESSAGEFORMATWARNING, CONFIGDEFAULT_TRACEMESSAGEFORMATWARNING); if (string.IsNullOrEmpty(_traceMessageFormatWarning)) { _traceMessageFormatWarning = _traceMessageFormat; }
                _traceMessageFormatWarning = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATWARNING, CONFIGDEFAULT_TRACEMESSAGEFORMATWARNING); if (string.IsNullOrEmpty(_traceMessageFormatWarning)) { _traceMessageFormatWarning = _traceMessageFormat; }
                //_traceMessageFormatError = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, string>(CONFIGSETTING_TRACEMESSAGEFORMATERROR, CONFIGDEFAULT_TRACEMESSAGEFORMATERROR); if (string.IsNullOrEmpty(_traceMessageFormatError)) { _traceMessageFormatError = _traceMessageFormat; }
                _traceMessageFormatError = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATERROR, CONFIGDEFAULT_TRACEMESSAGEFORMATERROR); if (string.IsNullOrEmpty(_traceMessageFormatError)) { _traceMessageFormatError = _traceMessageFormat; }
                //_traceMessageFormatCritical = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, string>(CONFIGSETTING_TRACEMESSAGEFORMATCRITICAL, CONFIGDEFAULT_TRACEMESSAGEFORMATCRITICAL); if (string.IsNullOrEmpty(_traceMessageFormatCritical)) { _traceMessageFormatCritical = _traceMessageFormat; }
                _traceMessageFormatCritical = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATCRITICAL, CONFIGDEFAULT_TRACEMESSAGEFORMATCRITICAL); if (string.IsNullOrEmpty(_traceMessageFormatCritical)) { _traceMessageFormatCritical = _traceMessageFormat; }
                //_traceMessageFormatCritical = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, string>(CONFIGSETTING_TRACEMESSAGEFORMATSTART, CONFIGDEFAULT_TRACEMESSAGEFORMATSTART); if (string.IsNullOrEmpty(_traceMessageFormatStart)) { _traceMessageFormatStart = _traceMessageFormat; }
                _traceMessageFormatCritical = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATSTART, CONFIGDEFAULT_TRACEMESSAGEFORMATSTART); if (string.IsNullOrEmpty(_traceMessageFormatStart)) { _traceMessageFormatStart = _traceMessageFormat; }
                //_traceMessageFormatStop = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, string>(CONFIGSETTING_TRACEMESSAGEFORMATSTOP, CONFIGDEFAULT_TRACEMESSAGEFORMATSTOP); if (string.IsNullOrEmpty(_traceMessageFormatStop)) { _traceMessageFormatStop = _traceMessageFormat; }
                _traceMessageFormatStop = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATSTOP, CONFIGDEFAULT_TRACEMESSAGEFORMATSTOP); if (string.IsNullOrEmpty(_traceMessageFormatStop)) { _traceMessageFormatStop = _traceMessageFormat; }
                //_traceMessageFormatInlineStop = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, string>(CONFIGSETTING_TRACEMESSAGEFORMATINLINESTOP, CONFIGDEFAULT_TRACEMESSAGEFORMATINLINESTOP); // if (string.IsNullOrEmpty(_traceMessageFormatInlineStop)) { _traceMessageFormatInlineStop = _traceMessageFormat; }
                _traceMessageFormatInlineStop = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATINLINESTOP, CONFIGDEFAULT_TRACEMESSAGEFORMATINLINESTOP); // if (string.IsNullOrEmpty(_traceMessageFormatInlineStop)) { _traceMessageFormatInlineStop = _traceMessageFormat; }
                //_traceMessageFormatSuspend = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, string>(CONFIGSETTING_TRACEMESSAGEFORMATSUSPEND, CONFIGDEFAULT_TRACEMESSAGEFORMATSUSPEND); if (string.IsNullOrEmpty(_traceMessageFormatSuspend)) { _traceMessageFormatSuspend = _traceMessageFormat; }
                _traceMessageFormatSuspend = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATSUSPEND, CONFIGDEFAULT_TRACEMESSAGEFORMATSUSPEND); if (string.IsNullOrEmpty(_traceMessageFormatSuspend)) { _traceMessageFormatSuspend = _traceMessageFormat; }
                //_traceMessageFormatResume = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, string>(CONFIGSETTING_TRACEMESSAGEFORMATRESUME, CONFIGDEFAULT_TRACEMESSAGEFORMATRESUME); if (string.IsNullOrEmpty(_traceMessageFormatResume)) { _traceMessageFormatResume = _traceMessageFormat; }
                _traceMessageFormatResume = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATRESUME, CONFIGDEFAULT_TRACEMESSAGEFORMATRESUME); if (string.IsNullOrEmpty(_traceMessageFormatResume)) { _traceMessageFormatResume = _traceMessageFormat; }
                //_traceMessageFormatTransfer = ConfigurationHelper.GetClassSetting<TraceListenerFormatItems, string>(CONFIGSETTING_TRACEMESSAGEFORMATTRANSFER, CONFIGDEFAULT_TRACEMESSAGEFORMATTRANSFER); if (string.IsNullOrEmpty(_traceMessageFormatTransfer)) { _traceMessageFormatTransfer = _traceMessageFormat; }
                _traceMessageFormatTransfer = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATTRANSFER, CONFIGDEFAULT_TRACEMESSAGEFORMATTRANSFER); if (string.IsNullOrEmpty(_traceMessageFormatTransfer)) { _traceMessageFormatTransfer = _traceMessageFormat; }

                var thicksPerMillisecond = TraceManager.Stopwatch.ElapsedTicks / TraceManager.Stopwatch.ElapsedMilliseconds;
                string fileName = null, workingDirectory = null;
                try { fileName = TraceLogger.CurrentProcess?.MainModule?.FileName; } catch { };
                try { workingDirectory = Directory.GetCurrentDirectory(); } catch { };

                scope.LogInformation($"Starting TraceListenerFormatItems for: ProcessName: '{TraceManager.ProcessName}', ProcessId: '{TraceManager.ProcessId}', FileName: '{fileName}', WorkingDirectory: '{workingDirectory}', EntryAssemblyFullName: '{TraceManager.EntryAssembly?.FullName}', ImageRuntimeVersion: '{TraceManager.EntryAssembly?.ImageRuntimeVersion}', Location: '{TraceManager.EntryAssembly?.Location}', thicksPerMillisecond: '{thicksPerMillisecond}'{Environment.NewLine}"); // "init"
                scope.LogDebug($"_filter '{_filter}', _categoryFilter '{_categoryFilter}', _allowedEventTypes '{_allowedEventTypes}', _showNestedFlow '{_showNestedFlow}', _flushOnWrite '{_flushOnWrite}', _cRReplace '{_CRReplace}', _lFReplace '{_LFReplace}', _timestampFormat '{_timestampFormat}'{Environment.NewLine}"); // "init"
                scope.LogDebug($"_processNamePadding '{_processNamePadding}', _sourcePadding '{_sourcePadding}', _categoryPadding '{_categoryPadding}', _sourceLevelPadding '{_sourceLevelPadding}'{Environment.NewLine}"); // "init"
                scope.LogDebug($"_traceMessageFormat '{_traceMessageFormat}', _traceMessageFormatVerbose '{_traceMessageFormatVerbose}', _traceMessageFormatWarning '{_traceMessageFormatWarning}', _traceMessageFormatError '{_traceMessageFormatError}', _traceMessageFormatCritical '{_traceMessageFormatCritical}', _traceMessageFormatStart '{_traceMessageFormatStart}', _traceMessageFormatStop '{_traceMessageFormatStop}, _traceMessageFormatInlineStop '{_traceMessageFormatInlineStop}'{Environment.NewLine}"); // "init"

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
                var variables = "now, processName, source, category, tidpid, sourceLevel, nesting, message, lastLineDelta, lastLineDeltaPadded, delta, deltaPadded, result, messageNesting".Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).Select((s) => new { name = s, position = i++ }).ToList();
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

        #region Filter
        Regex _filterRegex = new Regex(FILTER_SPLIT_REGEX, RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);
        string[] _filterCanInclude;
        string[] _filterMustInclude;
        string[] _filterMustExclude;
        string ISupportFilters.Filter
        {
            get { return _filter; }
            set
            {
                _filter = value;
                if (string.IsNullOrEmpty(_filter)) { _filterCanInclude = null; _filterMustInclude = null; _filterMustExclude = null; }
                var filterCanInclude = new List<string>();
                var filterMustInclude = new List<string>();
                var filterMustExclude = new List<string>();
                var items = _filterRegex.Split(_filter);
                items.ForEach(i =>
                {
                    if (i.StartsWith("!")) { var substr = i.Substring(1); if (!string.IsNullOrEmpty(substr)) { if (substr.IndexOf(' ') >= 0) { substr = substr.Trim('"'); } filterMustInclude.Add(substr); } return; }
                    if (i.StartsWith("-")) { var substr = i.Substring(1); if (!string.IsNullOrEmpty(substr)) { if (substr.IndexOf(' ') >= 0) { substr = substr.Trim('"'); } filterMustExclude.Add(substr); } return; }

                    if (i.IndexOf(' ') >= 0) { i = i.Trim('"'); }
                    if (!string.IsNullOrEmpty(i)) filterCanInclude.Add(i);
                });
                _filterCanInclude = filterCanInclude.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                _filterMustInclude = filterMustInclude.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                _filterMustExclude = filterMustExclude.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            }
        }
        #endregion
        #region CategoryFilter
        Regex _categoryFilterRegex = new Regex(FILTER_SPLIT_REGEX, RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);
        string[] _categoryFilterCanInclude;
        string[] _categoryFilterMustInclude;
        string[] _categoryFilterMustExclude;
        string CategoryFilter
        {
            get { return _categoryFilter; }
            set
            {
                _categoryFilter = value;
                if (string.IsNullOrEmpty(_categoryFilter)) { _categoryFilterCanInclude = null; _categoryFilterMustInclude = null; _categoryFilterMustExclude = null; }
                var categoryFilterCanInclude = new List<string>();
                var categoryFilterMustInclude = new List<string>();
                var categoryFilterMustExclude = new List<string>();
                var items = _categoryFilterRegex.Split(_categoryFilter);
                items.ForEach(i =>
                {
                    if (i.StartsWith("!")) { var substr = i.Substring(1); if (!string.IsNullOrEmpty(substr)) { if (substr.IndexOf(' ') >= 0) { substr = substr.Trim('"'); } categoryFilterMustInclude.Add(substr); } return; }
                    if (i.StartsWith("-")) { var substr = i.Substring(1); if (!string.IsNullOrEmpty(substr)) { if (substr.IndexOf(' ') >= 0) { substr = substr.Trim('"'); } categoryFilterMustExclude.Add(substr); } return; }

                    if (i.IndexOf(' ') >= 0) { i = i.Trim('"'); }
                    if (!string.IsNullOrEmpty(i)) categoryFilterCanInclude.Add(i);
                });
                _categoryFilterCanInclude = categoryFilterCanInclude.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                _categoryFilterMustInclude = categoryFilterMustInclude.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                _categoryFilterMustExclude = categoryFilterMustExclude.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            }
        }
        #endregion
        #region ISupportInnerListener
        public TraceListener InnerListener { get; set; }
        #endregion

        public override void Write(string s) { Write((object)s); }
        public override void Write(object o)
        {
            if (InnerListener == null) { return; }

            var entries = new List<object>();
            if (o is IEnumerable<object>) { entries.AddRange((o as IEnumerable<object>)); }
            if (o is TraceEntry) { entries.Add((TraceEntry)o); }
            if (o is string) { entries.Add(o as string); }

            var sbMessages = new StringBuilder();

            var a = entries.OfTypeChecked<object>().ForEach(e =>
            {
                var entry = default(TraceEntry);
                var message = e as string;
                var category = "general";
                var isLastWriteContinuation = false;

                if (e is TraceEntry)
                {
                    entry = (TraceEntry)e;
                    if (!_allowedEventTypes.HasFlag(entry.TraceEventType)) { return; }
                    category = entry.Category ?? category;
                }

                if (base.Filter != null && !base.Filter.ShouldTrace(null, null, entry.TraceEventType != 0 ? entry.TraceEventType : TraceEventType.Verbose, 0, null, null, null, null)) { return; }

                // check the category filter
                if (_categoryFilterMustInclude != null && _categoryFilterMustInclude.Length > 0 && _categoryFilterMustInclude.Any(categoryFilterMustInclude => category == null || category.IndexOf(categoryFilterMustInclude, StringComparison.CurrentCultureIgnoreCase) < 0)) { return; }
                if (_categoryFilterMustExclude != null && _categoryFilterMustExclude.Length > 0 && _categoryFilterMustExclude.Any(categoryFilterMustExclude => category != null && category.IndexOf(categoryFilterMustExclude, StringComparison.CurrentCultureIgnoreCase) >= 0)) { return; }
                if (_categoryFilterCanInclude != null && _categoryFilterCanInclude.Length > 0)
                {
                    if (_categoryFilterCanInclude.All(categoryFilterCanInclude => category == null || category.IndexOf(categoryFilterCanInclude, StringComparison.CurrentCultureIgnoreCase) < 0)) { return; }
                }

                if (e is TraceEntry)
                {
                    message = getEntryMessage(entry, lastWrite, out isLastWriteContinuation);
                    message += Environment.NewLine;
                }
                else if (e is string) { message = e as string; }
                else if (e != null) { message = e.ToString(); }

                // check the global filter
                if (_filterMustInclude != null && _filterMustInclude.Length > 0 && _filterMustInclude.Any(filterMustInclude => message == null || message.IndexOf(filterMustInclude, StringComparison.CurrentCultureIgnoreCase) < 0)) { return; }
                if (_filterMustExclude != null && _filterMustExclude.Length > 0 && _filterMustExclude.Any(filterMustExclude => message != null && message.IndexOf(filterMustExclude, StringComparison.CurrentCultureIgnoreCase) >= 0)) { return; }
                if (_filterCanInclude != null && _filterCanInclude.Length > 0)
                {
                    if (_filterCanInclude.All(filterCanInclude => message == null || message.IndexOf(filterCanInclude, StringComparison.CurrentCultureIgnoreCase) < 0)) { return; }
                }

                if (isLastWriteContinuation) { sbMessages.Remove(sbMessages.Length - 2, 2); }

                lastWrite = entry;
                if (!(e is TraceEntry)) { lastWrite.ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds; }

                var traceDeltaPadded = default(string);
                if (e is TraceEntry)
                {
                    //, TraceStartTicks = startTicks
                    var endTraceTicks = TraceManager.Stopwatch.ElapsedTicks;
                    traceDeltaPadded = (endTraceTicks - entry.TraceStartTicks).ToString("###0"); // .PadLeft(5)
                    if (traceDeltaPadded != null && traceDeltaPadded.Length < _traceDeltaPadding) { traceDeltaPadded = traceDeltaPadded.PadLeft(_traceDeltaPadding); }
                    if (traceDeltaPadded.Length > _traceDeltaPadding) { _traceDeltaPadding = traceDeltaPadded.Length; _traceDeltaDefault = new string(' ', _traceDeltaPadding); }
                }
                else { traceDeltaPadded = _traceDeltaDefault; };

                var fullMessage = _showTraceCost ? $"{traceDeltaPadded}.{message}" : message;
                sbMessages.Append(fullMessage);
            });

            if (sbMessages.Length > 0)
            {
                InnerListener.Write(sbMessages);
                if (_flushOnWrite) { this.Flush(); }
            }
        }

        public override void WriteLine(string s) { WriteLine((object)s); }
        public override void WriteLine(object o)
        {
            if (InnerListener == null) { return; }

            var entries = new List<object>();
            if (o is IEnumerable<object>) { entries.AddRange((o as IEnumerable<object>)); }
            if (o is TraceEntry) { entries.Add((TraceEntry)o); }
            if (o is string) { entries.Add(o as string); }

            var sbMessages = new StringBuilder();
            var a = entries.OfTypeChecked<object>().ForEach(e =>
            {
                var entry = default(TraceEntry);
                var message = e as string;
                var category = "general";
                var isLastWriteContinuation = false;
                if (e is TraceEntry)
                {
                    entry = (TraceEntry)e;
                    if (!_allowedEventTypes.HasFlag(entry.TraceEventType)) { return; }
                    category = entry.Category ?? category;
                }
                if (base.Filter != null && !base.Filter.ShouldTrace(null, null, entry.TraceEventType != 0 ? entry.TraceEventType : TraceEventType.Verbose, 0, null, null, null, null)) { return; }

                // check the category filter
                if (_categoryFilterMustInclude != null && _categoryFilterMustInclude.Length > 0 && _categoryFilterMustInclude.Any(categoryFilterMustInclude => category == null || category.IndexOf(categoryFilterMustInclude, StringComparison.CurrentCultureIgnoreCase) < 0)) { return; }
                if (_categoryFilterMustExclude != null && _categoryFilterMustExclude.Length > 0 && _categoryFilterMustExclude.Any(categoryFilterMustExclude => category != null && category.IndexOf(categoryFilterMustExclude, StringComparison.CurrentCultureIgnoreCase) >= 0)) { return; }
                if (_categoryFilterCanInclude != null && _categoryFilterCanInclude.Length > 0)
                {
                    if (category == null || _categoryFilterCanInclude.All(categoryFilterCanInclude => category.IndexOf(categoryFilterCanInclude, StringComparison.CurrentCultureIgnoreCase) < 0)) { return; }
                }

                if (e is TraceEntry) { message = getEntryMessage(entry, lastWrite, out isLastWriteContinuation); }
                else if (e is string) { message = e as string; }
                else if (e != null) { message = e.ToString(); }
                // message = message + Environment.NewLine;

                // check the global filter
                if (_filterMustInclude != null && _filterMustInclude.Length > 0 && _filterMustInclude.Any(filterMustInclude => message == null || message.IndexOf(filterMustInclude, StringComparison.CurrentCultureIgnoreCase) < 0)) { return; }
                if (_filterMustExclude != null && _filterMustExclude.Length > 0 && _filterMustExclude.Any(filterMustExclude => message != null && message.IndexOf(filterMustExclude, StringComparison.CurrentCultureIgnoreCase) >= 0)) { return; }
                if (_filterCanInclude != null && _filterCanInclude.Length > 0)
                {
                    if (message == null || _filterCanInclude.All(filterCanInclude => message.IndexOf(filterCanInclude, StringComparison.CurrentCultureIgnoreCase) < 0)) { return; }
                }

                // if TraceEntry stop && last write TraceEntry start Write
                if (isLastWriteContinuation) { sbMessages.Remove(sbMessages.Length - 2, 2); }

                lastWrite = entry;
                if (!(e is TraceEntry)) { lastWrite.ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds; }

                var traceDeltaPadded = default(string);
                if (e is TraceEntry)
                {
                    //, TraceStartTicks = startTicks
                    var endTraceTicks = TraceManager.Stopwatch.ElapsedTicks;
                    traceDeltaPadded = (endTraceTicks - entry.TraceStartTicks).ToString("###0"); // .PadLeft(5)
                    if (traceDeltaPadded != null && traceDeltaPadded.Length < _traceDeltaPadding) { traceDeltaPadded = traceDeltaPadded.PadLeft(_traceDeltaPadding); }
                    if (traceDeltaPadded.Length > _traceDeltaPadding) { _traceDeltaPadding = traceDeltaPadded.Length; _traceDeltaDefault = new string(' ', _traceDeltaPadding); }
                }
                else { traceDeltaPadded = _traceDeltaDefault; };

                var fullMessage = _showTraceCost ? $"{traceDeltaPadded}.{message}" : message;
                sbMessages.Append(message);
            });

            if (sbMessages.Length > 0)
            {
                InnerListener.WriteLine(sbMessages);
                if (_flushOnWrite) { this.Flush(); }
            }
        }
        public override void Flush()
        {

        }
        public string getEntryMessage(TraceEntry entry, TraceEntry lastWrite, out bool isLastWriteContinuation)
        {
            isLastWriteContinuation = false;
            string line = null;
            string category = entry.Category ?? "general";
            var processName = TraceManager.ProcessName + ".exe";
            var source = entry.Source ?? "";
            var codeSection = entry.CodeSection;
            if (processName != null && processName.Length < _processNamePadding) { processName = processName.PadRight(_processNamePadding); }
            if (source != null && source.Length < _sourcePadding) { source = source.PadRight(_sourcePadding); }
            if (source.Length > _sourcePadding) { _sourcePadding = source.Length; }
            if (category != null && category.Length < _categoryPadding) { category = category.PadRight(_categoryPadding); }
            if (category.Length > _categoryPadding) { _categoryPadding = category.Length; }
            var sourceLevel = entry.SourceLevel.ToString();
            if (sourceLevel != null && sourceLevel.Length < _sourceLevelPadding) { sourceLevel = sourceLevel.PadRight(_sourceLevelPadding); }
            if (sourceLevel.Length > _sourceLevelPadding) { _sourceLevelPadding = sourceLevel.Length; }

            //var tidpid = string.Format("{0,5} {1,4} {2}", TraceManager.ProcessId, entry.ThreadID, entry.ApartmentState);
            var tidpid = TraceLogger.ProcessId > 0 ? $"{TraceLogger.ProcessId,5} {entry.ThreadID,4}" : $"{entry.ThreadID,4}";
            if (entry.ApartmentState != ApartmentState.Unknown) { tidpid += $" {entry.ApartmentState}"; }

            var maxMessageLen = TraceManager.GetMaxMessageLen(codeSection, entry.TraceEventType);

            var messageRaw = entry.Message;
            if (entry.GetMessage != null) { messageRaw = entry.GetMessage(); }
            else if (entry.MessageFormat != null) { messageRaw = string.Format(entry.MessageFormat, entry.MessageArgs); }
            else { messageRaw = entry.MessageObject.GetLogString(); }

            var message = codeSection.IsInnerScope ? "... " + messageRaw : messageRaw;
            if (maxMessageLen > 0 && message != null && message.Length > maxMessageLen) { message = message.Substring(0, maxMessageLen.Value - 3) + "..."; }

            var nesting = getNesting(entry);
            string operationID = !string.IsNullOrEmpty(entry.RequestContext?.RequestId) ? entry.RequestContext?.RequestId : "null";
            var now = DateTime.Now.ToString(_timestampFormat);

            var delta = ""; var lastLineDelta = ""; var lastLineDeltaSB = new StringBuilder();
            var deltaPadded = ""; var lastLineDeltaPadded = "";
            var resultString = "";
            var messageNesting = _showNestedFlow ? new string(' ', entry.CodeSection != null ? entry.CodeSection.NestingLevel * 2 : 0) : "";

            lastLineDeltaPadded = lastLineDelta = getLastLineDeltaOptimized(entry, lastWrite); // .PadLeft(5)
            if (lastLineDeltaPadded != null && lastLineDeltaPadded.Length < _deltaPadding) { lastLineDeltaPadded = lastLineDeltaPadded.PadLeft(_deltaPadding); }
            if (lastLineDeltaPadded.Length > _deltaPadding) { _deltaPadding = lastLineDeltaPadded.Length; }

            var linePrefix = ""; var lineSuffix = "";
            switch (entry.TraceEventType)
            {
                case TraceEventType.Start:
                    // line = $"[{now}] {processName} {source} {category} {tidpid} - {sourceLevel} - {nesting} {message}"; "[{0}] {1} {2} {3} - {4,-11} - {5} {6}"
                    string section = !string.IsNullOrEmpty(codeSection.Name) ? string.Format(".{0}", codeSection.Name) : null;
                    message = $"{codeSection.T?.Name}.{codeSection.MemberName}{section}() START ";
                    if (codeSection.Payload != null)
                    {
                        var payload = codeSection.Payload;
                        if (payload is Delegate)
                        {
                            try { payload = ((Delegate)codeSection.Payload).DynamicInvoke(); }
                            catch (Exception) { /*payload = ex; */}
                        }

                        var maxPayloadLen = maxMessageLen >= 0 ? (int)maxMessageLen - Min((int)maxMessageLen, message.Length) : -1;
                        var payloadString = $"{payload.GetLogString()}";
                        if (payloadString.Length > maxPayloadLen)
                        {
                            var deltaLen = maxPayloadLen > 3 ? maxPayloadLen - 3 : maxPayloadLen;
                            if (deltaLen > 0) { payloadString = payloadString.Substring(0, deltaLen) + "..."; }
                        }
                        message = $"{codeSection.T?.Name}.{codeSection.MemberName}{section}({payloadString}) START ";
                    }

                    deltaPadded = delta = ""; // .PadLeft(5)
                    if (deltaPadded != null && deltaPadded.Length < _deltaPadding) { deltaPadded = deltaPadded.PadLeft(_deltaPadding); }
                    if (deltaPadded.Length > _deltaPadding) { _deltaPadding = deltaPadded.Length; }

                    if (!string.IsNullOrEmpty(_traceMessageFormatPrefix)) { linePrefix = string.Format(_traceMessageFormatPrefix, now, processName, source, category, tidpid, sourceLevel, nesting, message, lastLineDelta, lastLineDeltaPadded, delta, deltaPadded, resultString, messageNesting); }
                    if (!string.IsNullOrEmpty(_traceMessageFormatStart)) { lineSuffix = string.Format(_traceMessageFormatStart, now, processName, source, category, tidpid, sourceLevel, nesting, message, lastLineDelta, lastLineDeltaPadded, delta, deltaPadded, resultString, messageNesting); }
                    line = $"{linePrefix}{lineSuffix}";
                    break;
                case TraceEventType.Stop:
                    // line = $"[{now}] {processName} {source} {category} {tidpid} - {sourceLevel} - {nesting} {message}"; "[{0}] {1} {2} {3} - {4,-11} - {5} {6} ({7} secs)"
                    section = !string.IsNullOrEmpty(codeSection.Name) ? string.Format(".{0}", codeSection.Name) : null;
                    message = string.Format("{0}.{1}{2}() END", !string.IsNullOrWhiteSpace(codeSection.T.Name) ? codeSection.T.Name : string.Empty, codeSection.MemberName, section);
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

                    var milliseconds = TraceManager.Stopwatch.ElapsedMilliseconds - codeSection.CallStartMilliseconds;
                    deltaPadded = delta = ((float)milliseconds / 1000).ToString("###0.00"); // .PadLeft(5)
                    if (deltaPadded != null && deltaPadded.Length < _deltaPadding) { deltaPadded = deltaPadded.PadLeft(_deltaPadding); }
                    if (deltaPadded.Length > _deltaPadding) { _deltaPadding = deltaPadded.Length; }

                    var traceMessageFormat = _traceMessageFormatStop;
                    if (_lastWriteContinuationEnabled == true && lastWrite.CodeSection == codeSection && lastWrite.TraceEventType == TraceEventType.Start)
                    {
                        isLastWriteContinuation = true;
                        traceMessageFormat = _traceMessageFormatInlineStop;
                    }

                    if (!string.IsNullOrEmpty(_traceMessageFormatPrefix)) { linePrefix = string.Format(_traceMessageFormatPrefix, now, processName, source, category, tidpid, sourceLevel, nesting, message, lastLineDelta, lastLineDeltaPadded, delta, deltaPadded, resultString, messageNesting); }
                    if (!string.IsNullOrEmpty(traceMessageFormat)) { lineSuffix = string.Format(traceMessageFormat, now, processName, source, category, tidpid, sourceLevel, nesting, message, lastLineDelta, lastLineDeltaPadded, delta, deltaPadded, resultString, messageNesting); }
                    line = $"{linePrefix}{lineSuffix}";
                    break;
                default: // case TraceEntryType.Message:
                    // line = $"[{now}] {processName} {source} {category} {tidpid} - {sourceLevel} - {nesting}   {message}"; "[{0}] {1} {2} {3} - {4,-11} - {5}   {6}"
                    deltaPadded = delta = ""; // .PadLeft(5)
                    if (deltaPadded != null && deltaPadded.Length < _deltaPadding) { deltaPadded = deltaPadded.PadLeft(_deltaPadding); }
                    if (deltaPadded.Length > _deltaPadding) { _deltaPadding = deltaPadded.Length; }
                    if (_showNestedFlow) { messageNesting += "  "; }
                    if (!string.IsNullOrEmpty(_traceMessageFormatPrefix)) { linePrefix = string.Format(_traceMessageFormatPrefix, now, processName, source, category, tidpid, sourceLevel, nesting, message, lastLineDelta, lastLineDeltaPadded, delta, deltaPadded, resultString, messageNesting); }
                    if (!string.IsNullOrEmpty(_traceMessageFormatInformation)) { lineSuffix = string.Format(_traceMessageFormatInformation, now, processName, source, category, tidpid, sourceLevel, nesting, message, lastLineDelta, lastLineDeltaPadded, delta, deltaPadded, resultString, messageNesting); }
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

            var section = entry.CodeSection;
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
        int Min(int a, int b) { return a < b ? a : b; }
        #endregion
    }

    public class TraceListenerDefault : TraceListenerFormatItems, ISupportFilters
    {
        public TraceListenerDefault() : base()
        {
            using (var sec = this.GetCodeSection())
            {
                TraceListener innerListener = new DefaultTraceListener();
                InnerListener = innerListener;
            }
        }
    }
    //
    //public class TraceListenerDefaultAzure : TraceListenerFormatItems, ISupportFilters
    //{
    //    public TraceListenerDefaultAzure() : base()
    //    {
    //        TraceListener innerListener = new AzureDriveTraceListener();
    //        InnerListener = innerListener;
    //    }
    //}
}
