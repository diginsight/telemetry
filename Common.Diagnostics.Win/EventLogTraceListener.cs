#region using
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Reflection;
#endregion

namespace Common
{
    public class EventLogTraceListener : TraceListener, ISupportFilters
    {
        #region const
        private const long ERROR_LOG_FILE_FULL = 0x5de;
        public const string FILTER_SPLIT_REGEX = @"([-!]?[\""].+?[\""])|([-!]?\w+)";
        public const string CONFIGSETTING_CRREPLACE = "CRReplace"; public const string CONFIGDEFAULT_CRREPLACE = "\\r";
        public const string CONFIGSETTING_LFREPLACE = "LFReplace"; public const string CONFIGDEFAULT_LFREPLACE = "\\n";
        public const string CONFIGSETTING_FILTER = "Filter"; public const string CONFIGDEFAULT_FILTER = null;
        public const string CONFIGSETTING_CATEGORYFILTER = "CategoryFilter"; public const string CONFIGDEFAULT_CATEGORYFILTER = null;
        public const string CONFIGSETTING_TIMESTAMPFORMAT = "TimestampFormat"; public const string CONFIGDEFAULT_TIMESTAMPFORMAT = "HH:mm:ss.fff"; // dd/MM/yyyy
        public const string CONFIGSETTING_FLUSHONWRITE = "FlushOnWrite"; public const bool CONFIGDEFAULT_FLUSHONWRITE = false;
        public const string CONFIGSETTING_SHOWNESTEDFLOW = "ShowNestedFlow"; public const bool CONFIGDEFAULT_SHOWNESTEDFLOW = false;
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
        public const string CONFIGSETTING_TRACEMESSAGEFORMAT = "TraceMessageFormat"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMAT = "[{now}] {source} {category} {tidpid} - {sourceLevel} - {lastLineDeltaPadded} {deltaPadded} {nesting}   {message}";
        public const string CONFIGSETTING_TRACEMESSAGEFORMATVERBOSE = "TraceMessageFormatVerbose"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATVERBOSE = null;
        public const string CONFIGSETTING_TRACEMESSAGEFORMATINFORMATION = "TraceMessageFormatInformation"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATINFORMATION = null;
        public const string CONFIGSETTING_TRACEMESSAGEFORMATWARNING = "TraceMessageFormatWarning"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATWARNING = null;
        public const string CONFIGSETTING_TRACEMESSAGEFORMATERROR = "TraceMessageFormatError"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATERROR = null;
        public const string CONFIGSETTING_TRACEMESSAGEFORMATCRITICAL = "TraceMessageFormatCritical"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATCRITICAL = null;
        public const string CONFIGSETTING_TRACEMESSAGEFORMATSTART = "TraceMessageFormatStart"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATSTART = "[{now}] {source} {category} {tidpid} - {sourceLevel} - {lastLineDeltaPadded} {deltaPadded} {nesting} {message}";
        public const string CONFIGSETTING_TRACEMESSAGEFORMATSTOP = "TraceMessageFormatStop"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATSTOP = "[{now}] {source} {category} {tidpid} - {sourceLevel} - {lastLineDeltaPadded} {deltaPadded} {nesting} {message}{result}";
        public const string CONFIGSETTING_TRACEMESSAGEFORMATINLINESTOP = "TraceMessageFormatInlineStop"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATINLINESTOP = "... END ({delta} secs){result}";
        public const string CONFIGSETTING_TRACEMESSAGEFORMATSUSPEND = "TraceMessageFormatSuspend"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATSUSPEND = null;
        public const string CONFIGSETTING_TRACEMESSAGEFORMATRESUME = "TraceMessageFormatResume"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATRESUME = null;
        public const string CONFIGSETTING_TRACEMESSAGEFORMATTRANSFER = "TraceMessageFormatTransfer"; public const string CONFIGDEFAULT_TRACEMESSAGEFORMATTRANSFER = null;
        #endregion
        #region internal state
        private static ClassConfigurationGetter<EventLogTraceListener> classConfigurationGetter;
        bool _lastWriteContinuationEnabled;
        public string _CRReplace, _LFReplace;
        public string _timestampFormat;
        public bool _showNestedFlow, _flushOnWrite;
        public int _processNamePadding, _sourcePadding, _categoryPadding, _sourceLevelPadding, _deltaPadding;
        public string _traceMessageFormat, _traceMessageFormatVerbose, _traceMessageFormatInformation, _traceMessageFormatWarning, _traceMessageFormatError, _traceMessageFormatCritical;
        public string _traceMessageFormatStart, _traceMessageFormatStop, _traceMessageFormatInlineStop, _traceMessageFormatSuspend, _traceMessageFormatResume, _traceMessageFormatTransfer;
        public string _filter, _categoryFilter;
        public TraceEventType _allowedEventTypes = TraceEventType.Critical | TraceEventType.Error | TraceEventType.Warning | TraceEventType.Information | TraceEventType.Verbose | TraceEventType.Start | TraceEventType.Stop | TraceEventType.Suspend | TraceEventType.Resume | TraceEventType.Transfer;
        private readonly int _timeout = 10;
        private Assembly _assembly = TraceManager.EntryAssembly;
        TraceEntry lastWrite = default(TraceEntry);
        #endregion

        #region .ctor
        public EventLogTraceListener() { Init(); }
        // Summary: Initializes a new instance of the EkipCommon.EventLogTraceListener class that writes to the specified output stream.
        // Parameters:  stream: The System.IO.Stream to receive the output.
        // Exceptions:  T:System.ArgumentNullException: stream is null.
        public EventLogTraceListener(Stream stream) : base() { Init(); }

        // Summary: Initializes a new instance of the EkipCommon.EventLogTraceListener class that writes to the specified text writer.
        // Parameters:  writer: The System.IO.TextWriter to receive the output.
        // Exceptions:  T:System.ArgumentNullException: writer is null.
        public EventLogTraceListener(TextWriter writer) { Init(); }
        // Summary: Initializes a new instance of the EkipCommon.EventLogTraceListener class that writes to the specified file.
        // Parameters:
        //   fileName: The name of the file to receive the output.
        // Exceptions: T:System.ArgumentNullException: fileName is null.
        public EventLogTraceListener(string fileName) : base() { Init(); }

        // Summary: Initializes a new instance of the EkipCommon.EventLogTraceListener
        //          class that writes to the specified output stream and has the specified name.
        // Parameters:
        //   stream: The System.IO.Stream to receive the output.
        //   name: The name of the new instance of the trace listener.
        // Exceptions: T:System.ArgumentNullException: stream is null.
        public EventLogTraceListener(Stream stream, string name) { Init(); }

        // Summary: Initializes a new instance of the EkipCommon.EventLogTraceListener
        //          class that writes to the specified text writer and has the specified name.
        // Parameters:
        //   writer: The System.IO.TextWriter to receive the output.
        //   name: The name of the new instance of the trace listener.
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     writer is null.
        public EventLogTraceListener(TextWriter writer, string name) { Init(); }

        // Summary: Initializes a new instance of the EkipCommon.EventLogTraceListener
        //          class that writes to the specified file and has the specified name.
        // Parameters:
        //   fileName: The name of the file to receive the output.
        //   name: The name of the new instance of the trace listener.
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     fileName is null.
        public EventLogTraceListener(string fileName, string name) { Init(); }
        #endregion

        private void Init()
        {
            if (classConfigurationGetter == null) { classConfigurationGetter = new ClassConfigurationGetter<EventLogTraceListener>(TraceLogger.Configuration); }

            //_CRReplace = ConfigurationHelper.GetClassSetting<EventLogTraceListener, string>(CONFIGSETTING_CRREPLACE, CONFIGDEFAULT_CRREPLACE);
            _CRReplace = classConfigurationGetter.Get(CONFIGSETTING_CRREPLACE, CONFIGDEFAULT_CRREPLACE);
            //_LFReplace = ConfigurationHelper.GetClassSetting<EventLogTraceListener, string>(CONFIGSETTING_LFREPLACE, CONFIGDEFAULT_LFREPLACE);  // ConfigurationHelper.GetSetting<int>(CONFIGSETTING_LFREPLACE, CONFIGDEFAULT_LFREPLACE);
            _LFReplace = classConfigurationGetter.Get(CONFIGSETTING_LFREPLACE, CONFIGDEFAULT_LFREPLACE);  // ConfigurationHelper.GetSetting<int>(CONFIGSETTING_LFREPLACE, CONFIGDEFAULT_LFREPLACE);
            //var filter = ConfigurationHelper.GetClassSetting<EventLogTraceListener, string>(CONFIGSETTING_FILTER, CONFIGDEFAULT_FILTER);
            var filter = classConfigurationGetter.Get(CONFIGSETTING_FILTER, CONFIGDEFAULT_FILTER);
            if (!string.IsNullOrEmpty(filter)) { ((ISupportFilters)this).Filter = filter; }
            //var categoryFilter = ConfigurationHelper.GetClassSetting<EventLogTraceListener, string>(CONFIGSETTING_CATEGORYFILTER, CONFIGDEFAULT_CATEGORYFILTER);
            var categoryFilter = classConfigurationGetter.Get(CONFIGSETTING_CATEGORYFILTER, CONFIGDEFAULT_CATEGORYFILTER);
            if (!string.IsNullOrEmpty(categoryFilter)) { this.CategoryFilter = categoryFilter; }

            //_timestampFormat = ConfigurationHelper.GetClassSetting<EventLogTraceListener, string>(CONFIGSETTING_TIMESTAMPFORMAT, CONFIGDEFAULT_TIMESTAMPFORMAT);  // ConfigurationHelper.GetSetting<int>(CONFIGSETTING_TIMESTAMPFORMAT, CONFIGDEFAULT_TIMESTAMPFORMAT);
            _timestampFormat = classConfigurationGetter.Get(CONFIGSETTING_TIMESTAMPFORMAT, CONFIGDEFAULT_TIMESTAMPFORMAT);  // ConfigurationHelper.GetSetting<int>(CONFIGSETTING_TIMESTAMPFORMAT, CONFIGDEFAULT_TIMESTAMPFORMAT);
            //_showNestedFlow = ConfigurationHelper.GetClassSetting<EventLogTraceListener, bool>(CONFIGSETTING_SHOWNESTEDFLOW, CONFIGDEFAULT_SHOWNESTEDFLOW);
            _showNestedFlow = classConfigurationGetter.Get(CONFIGSETTING_SHOWNESTEDFLOW, CONFIGDEFAULT_SHOWNESTEDFLOW);
            //_flushOnWrite = ConfigurationHelper.GetClassSetting<EventLogTraceListener, bool>(CONFIGSETTING_FLUSHONWRITE, CONFIGDEFAULT_FLUSHONWRITE);
            _flushOnWrite = classConfigurationGetter.Get(CONFIGSETTING_FLUSHONWRITE, CONFIGDEFAULT_FLUSHONWRITE);
            //_processNamePadding = ConfigurationHelper.GetClassSetting<EventLogTraceListener, int>(CONFIGSETTING_PROCESSNAMEPADDING, CONFIGDEFAULT_PROCESSNAMEPADDING);
            _processNamePadding = classConfigurationGetter.Get(CONFIGSETTING_PROCESSNAMEPADDING, CONFIGDEFAULT_PROCESSNAMEPADDING);
            //_sourcePadding = ConfigurationHelper.GetClassSetting<EventLogTraceListener, int>(CONFIGSETTING_SOURCEPADDING, CONFIGDEFAULT_SOURCEPADDING);
            _sourcePadding = classConfigurationGetter.Get(CONFIGSETTING_SOURCEPADDING, CONFIGDEFAULT_SOURCEPADDING);
            //_categoryPadding = ConfigurationHelper.GetClassSetting<EventLogTraceListener, int>(CONFIGSETTING_CATEGORYPADDING, CONFIGDEFAULT_CATEGORYPADDING);
            _categoryPadding = classConfigurationGetter.Get(CONFIGSETTING_CATEGORYPADDING, CONFIGDEFAULT_CATEGORYPADDING);
            //_sourceLevelPadding = ConfigurationHelper.GetClassSetting<EventLogTraceListener, int>(CONFIGSETTING_SOURCELEVELPADDING, CONFIGDEFAULT_SOURCELEVELPADDING);
            _sourceLevelPadding = classConfigurationGetter.Get(CONFIGSETTING_SOURCELEVELPADDING, CONFIGDEFAULT_SOURCELEVELPADDING);
            //_deltaPadding = ConfigurationHelper.GetClassSetting<EventLogTraceListener, int>(CONFIGSETTING_DELTAPADDING, CONFIGDEFAULT_DELTAPADDING);
            _deltaPadding = classConfigurationGetter.Get(CONFIGSETTING_DELTAPADDING, CONFIGDEFAULT_DELTAPADDING);
            //_lastWriteContinuationEnabled = ConfigurationHelper.GetClassSetting<EventLogTraceListener, bool>(CONFIGSETTING_LASTWRITECONTINUATIONENABLED, CONFIGDEFAULT_LASTWRITECONTINUATIONENABLED);
            _lastWriteContinuationEnabled = classConfigurationGetter.Get(CONFIGSETTING_LASTWRITECONTINUATIONENABLED, CONFIGDEFAULT_LASTWRITECONTINUATIONENABLED);

            //_traceMessageFormat = ConfigurationHelper.GetClassSetting<EventLogTraceListener, string>(CONFIGSETTING_TRACEMESSAGEFORMAT, CONFIGDEFAULT_TRACEMESSAGEFORMAT);
            _traceMessageFormat = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMAT, CONFIGDEFAULT_TRACEMESSAGEFORMAT);
            //_traceMessageFormatVerbose = ConfigurationHelper.GetClassSetting<EventLogTraceListener, string>(CONFIGSETTING_TRACEMESSAGEFORMATVERBOSE, CONFIGDEFAULT_TRACEMESSAGEFORMATVERBOSE); if (string.IsNullOrEmpty(_traceMessageFormatVerbose)) { _traceMessageFormatVerbose = _traceMessageFormat; }
            _traceMessageFormatVerbose = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATVERBOSE, CONFIGDEFAULT_TRACEMESSAGEFORMATVERBOSE); if (string.IsNullOrEmpty(_traceMessageFormatVerbose)) { _traceMessageFormatVerbose = _traceMessageFormat; }
            //_traceMessageFormatInformation = ConfigurationHelper.GetClassSetting<EventLogTraceListener, string>(CONFIGSETTING_TRACEMESSAGEFORMATINFORMATION, CONFIGDEFAULT_TRACEMESSAGEFORMATINFORMATION); if (string.IsNullOrEmpty(_traceMessageFormatInformation)) { _traceMessageFormatInformation = _traceMessageFormat; }
            _traceMessageFormatInformation = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATINFORMATION, CONFIGDEFAULT_TRACEMESSAGEFORMATINFORMATION); if (string.IsNullOrEmpty(_traceMessageFormatInformation)) { _traceMessageFormatInformation = _traceMessageFormat; }
            //_traceMessageFormatWarning = ConfigurationHelper.GetClassSetting<EventLogTraceListener, string>(CONFIGSETTING_TRACEMESSAGEFORMATWARNING, CONFIGDEFAULT_TRACEMESSAGEFORMATWARNING); if (string.IsNullOrEmpty(_traceMessageFormatWarning)) { _traceMessageFormatWarning = _traceMessageFormat; }
            _traceMessageFormatWarning = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATWARNING, CONFIGDEFAULT_TRACEMESSAGEFORMATWARNING); if (string.IsNullOrEmpty(_traceMessageFormatWarning)) { _traceMessageFormatWarning = _traceMessageFormat; }
            //_traceMessageFormatError = ConfigurationHelper.GetClassSetting<EventLogTraceListener, string>(CONFIGSETTING_TRACEMESSAGEFORMATERROR, CONFIGDEFAULT_TRACEMESSAGEFORMATERROR); if (string.IsNullOrEmpty(_traceMessageFormatError)) { _traceMessageFormatError = _traceMessageFormat; }
            _traceMessageFormatError = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATERROR, CONFIGDEFAULT_TRACEMESSAGEFORMATERROR); if (string.IsNullOrEmpty(_traceMessageFormatError)) { _traceMessageFormatError = _traceMessageFormat; }
            //_traceMessageFormatCritical = ConfigurationHelper.GetClassSetting<EventLogTraceListener, string>(CONFIGSETTING_TRACEMESSAGEFORMATCRITICAL, CONFIGDEFAULT_TRACEMESSAGEFORMATCRITICAL); if (string.IsNullOrEmpty(_traceMessageFormatCritical)) { _traceMessageFormatCritical = _traceMessageFormat; }
            _traceMessageFormatCritical = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATCRITICAL, CONFIGDEFAULT_TRACEMESSAGEFORMATCRITICAL); if (string.IsNullOrEmpty(_traceMessageFormatCritical)) { _traceMessageFormatCritical = _traceMessageFormat; }

            //_traceMessageFormatStart = ConfigurationHelper.GetClassSetting<EventLogTraceListener, string>(CONFIGSETTING_TRACEMESSAGEFORMATSTART, CONFIGDEFAULT_TRACEMESSAGEFORMATSTART); if (string.IsNullOrEmpty(_traceMessageFormatStart)) { _traceMessageFormatStart = _traceMessageFormat; }
            _traceMessageFormatStart = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATSTART, CONFIGDEFAULT_TRACEMESSAGEFORMATSTART); if (string.IsNullOrEmpty(_traceMessageFormatStart)) { _traceMessageFormatStart = _traceMessageFormat; }
            //_traceMessageFormatStop = ConfigurationHelper.GetClassSetting<EventLogTraceListener, string>(CONFIGSETTING_TRACEMESSAGEFORMATSTOP, CONFIGDEFAULT_TRACEMESSAGEFORMATSTOP); if (string.IsNullOrEmpty(_traceMessageFormatStop)) { _traceMessageFormatStop = _traceMessageFormat; }
            _traceMessageFormatStop = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATSTOP, CONFIGDEFAULT_TRACEMESSAGEFORMATSTOP); if (string.IsNullOrEmpty(_traceMessageFormatStop)) { _traceMessageFormatStop = _traceMessageFormat; }
            //_traceMessageFormatInlineStop = ConfigurationHelper.GetClassSetting<EventLogTraceListener, string>(CONFIGSETTING_TRACEMESSAGEFORMATINLINESTOP, CONFIGDEFAULT_TRACEMESSAGEFORMATINLINESTOP); // if (string.IsNullOrEmpty(_traceMessageFormatInlineStop)) { _traceMessageFormatInlineStop = _traceMessageFormat; }
            _traceMessageFormatInlineStop = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATINLINESTOP, CONFIGDEFAULT_TRACEMESSAGEFORMATINLINESTOP); // if (string.IsNullOrEmpty(_traceMessageFormatInlineStop)) { _traceMessageFormatInlineStop = _traceMessageFormat; }
            //_traceMessageFormatSuspend = ConfigurationHelper.GetClassSetting<EventLogTraceListener, string>(CONFIGSETTING_TRACEMESSAGEFORMATSUSPEND, CONFIGDEFAULT_TRACEMESSAGEFORMATSUSPEND); if (string.IsNullOrEmpty(_traceMessageFormatSuspend)) { _traceMessageFormatSuspend = _traceMessageFormat; }
            _traceMessageFormatSuspend = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATSUSPEND, CONFIGDEFAULT_TRACEMESSAGEFORMATSUSPEND); if (string.IsNullOrEmpty(_traceMessageFormatSuspend)) { _traceMessageFormatSuspend = _traceMessageFormat; }
            //_traceMessageFormatResume = ConfigurationHelper.GetClassSetting<EventLogTraceListener, string>(CONFIGSETTING_TRACEMESSAGEFORMATRESUME, CONFIGDEFAULT_TRACEMESSAGEFORMATRESUME); if (string.IsNullOrEmpty(_traceMessageFormatResume)) { _traceMessageFormatResume = _traceMessageFormat; }
            _traceMessageFormatResume = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATRESUME, CONFIGDEFAULT_TRACEMESSAGEFORMATRESUME); if (string.IsNullOrEmpty(_traceMessageFormatResume)) { _traceMessageFormatResume = _traceMessageFormat; }
            //_traceMessageFormatTransfer = ConfigurationHelper.GetClassSetting<EventLogTraceListener, string>(CONFIGSETTING_TRACEMESSAGEFORMATTRANSFER, CONFIGDEFAULT_TRACEMESSAGEFORMATTRANSFER); if (string.IsNullOrEmpty(_traceMessageFormatTransfer)) { _traceMessageFormatTransfer = _traceMessageFormat; }
            _traceMessageFormatTransfer = classConfigurationGetter.Get(CONFIGSETTING_TRACEMESSAGEFORMATTRANSFER, CONFIGDEFAULT_TRACEMESSAGEFORMATTRANSFER); if (string.IsNullOrEmpty(_traceMessageFormatTransfer)) { _traceMessageFormatTransfer = _traceMessageFormat; }

            var thicksPerMillisecond = TraceManager.Stopwatch.ElapsedTicks / TraceManager.Stopwatch.ElapsedMilliseconds;
            string fileName = null, workingDirectory = null;
            try { fileName = TraceManager.CurrentProcess?.StartInfo?.FileName; } catch { };
            try { workingDirectory = TraceManager.CurrentProcess.StartInfo.WorkingDirectory; } catch { };

            this.Write($"Starting EventLogTraceListener for: ProcessName: '{TraceManager.ProcessName}', ProcessId: '{TraceManager.ProcessId}', FileName: '{fileName}', WorkingDirectory: '{workingDirectory}', EntryAssemblyFullName: '{TraceManager.EntryAssembly?.FullName}', ImageRuntimeVersion: '{TraceManager.EntryAssembly?.ImageRuntimeVersion}', Location: '{TraceManager.EntryAssembly?.Location}', thicksPerMillisecond: '{thicksPerMillisecond}'{Environment.NewLine}"); // "init"
            this.Write($"_filter '{_filter}', _categoryFilter '{_categoryFilter}', _allowedEventTypes '{_allowedEventTypes}', _showNestedFlow '{_showNestedFlow}', _flushOnWrite '{_flushOnWrite}', _cRReplace '{_CRReplace}', _lFReplace '{_LFReplace}', _timestampFormat '{_timestampFormat}'{Environment.NewLine}"); // "init"
            this.Write($"_processNamePadding '{_processNamePadding}', _sourcePadding '{_sourcePadding}', _categoryPadding '{_categoryPadding}', _sourceLevelPadding '{_sourceLevelPadding}'{Environment.NewLine}"); // "init"
            this.Write($"_traceMessageFormat '{_traceMessageFormat}', _traceMessageFormatVerbose '{_traceMessageFormatVerbose}', _traceMessageFormatWarning '{_traceMessageFormatWarning}', _traceMessageFormatError '{_traceMessageFormatError}', _traceMessageFormatCritical '{_traceMessageFormatCritical}', _traceMessageFormatStart '{_traceMessageFormatStart}', _traceMessageFormatStop '{_traceMessageFormatStop}, _traceMessageFormatInlineStop '{_traceMessageFormatInlineStop}'{Environment.NewLine}"); // "init"
            //this.Write($"{Environment.NewLine}"); // "init"

            int i = 0;
            var variables = "now, processName, source, category, tidpid, sourceLevel, nesting, message, lastLineDelta, lastLineDeltaPadded, delta, deltaPadded, result".Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).Select((s) => new { name = s, position = i++ }).ToList();
            variables.ForEach(v =>
            {
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

        public override void Write(string s) { Write((object)s); }
        public override void Write(object o)
        {
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

                if (e is TraceEntry)
                {
                    message = getEntryMessage(entry, lastWrite, out isLastWriteContinuation);
                    message += Environment.NewLine;
                    lastWrite = entry;
                }
                else if (e is string) { message = e as string; lastWrite = default(TraceEntry); lastWrite.ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds; }
                else if (e != null) { message = e.ToString(); lastWrite = default(TraceEntry); lastWrite.ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds; }
                else { lastWrite = default(TraceEntry); lastWrite.ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds; }

                // check the global filter
                if (_filterMustInclude != null && _filterMustInclude.Length > 0 && _filterMustInclude.Any(filterMustInclude => message == null || message.IndexOf(filterMustInclude, StringComparison.CurrentCultureIgnoreCase) < 0)) { return; }
                if (_filterMustExclude != null && _filterMustExclude.Length > 0 && _filterMustExclude.Any(filterMustExclude => message != null && message.IndexOf(filterMustExclude, StringComparison.CurrentCultureIgnoreCase) >= 0)) { return; }
                if (_filterCanInclude != null && _filterCanInclude.Length > 0)
                {
                    if (message == null || _filterCanInclude.All(filterCanInclude => message.IndexOf(filterCanInclude, StringComparison.CurrentCultureIgnoreCase) < 0)) { return; }
                }

                if (isLastWriteContinuation) { sbMessages.Remove(sbMessages.Length - 2, 2); }

                try { EventLog.WriteEntry(_assembly.GetName().Name, message, trace2EventlogEntryType(entry.TraceEventType)); } catch (System.ComponentModel.Win32Exception ex) { if (ex.NativeErrorCode != ERROR_LOG_FILE_FULL) throw; } // ignores some errors
            });
        }

        public override void WriteLine(string s) { WriteLine((object)s); }
        public override void WriteLine(object o)
        {
            var entries = new List<object>();
            if (o is IEnumerable<object>) { entries.AddRange((o as IEnumerable<object>)); }
            if (o is TraceEntry) { entries.Add((TraceEntry)o); }
            if (o is string) { entries.Add(o as string); }

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
                    entry = (TraceEntry)e;
                    message = getEntryMessage(entry, lastWrite, out isLastWriteContinuation);
                    lastWrite = entry;
                }
                else if (e is string) { message = e as string; lastWrite = default(TraceEntry); lastWrite.ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds; }
                else if (e != null) { message = e.ToString(); lastWrite = default(TraceEntry); lastWrite.ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds; }
                else { lastWrite = default(TraceEntry); lastWrite.ElapsedMilliseconds = TraceManager.Stopwatch.ElapsedMilliseconds; }
                message = message + Environment.NewLine;

                // check the global filter
                if (_filterMustInclude != null && _filterMustInclude.Length > 0 && _filterMustInclude.Any(filterMustInclude => message == null || message.IndexOf(filterMustInclude, StringComparison.CurrentCultureIgnoreCase) < 0)) { return; }
                if (_filterMustExclude != null && _filterMustExclude.Length > 0 && _filterMustExclude.Any(filterMustExclude => message != null && message.IndexOf(filterMustExclude, StringComparison.CurrentCultureIgnoreCase) >= 0)) { return; }
                if (_filterCanInclude != null && _filterCanInclude.Length > 0)
                {
                    if (_filterCanInclude.All(filterCanInclude => message == null || message.IndexOf(filterCanInclude, StringComparison.CurrentCultureIgnoreCase) < 0)) { return; }
                }

                try { EventLog.WriteEntry(_assembly.GetName().Name, message, trace2EventlogEntryType(entry.TraceEventType)); } catch (System.ComponentModel.Win32Exception ex) { if (ex.NativeErrorCode != ERROR_LOG_FILE_FULL) throw; } // ignores some errors
            });
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

            var tidpid = string.Format("{0,5} {1,4} {2}", TraceManager.ProcessId, entry.ThreadID, entry.ApartmentState);
            var maxMessageLen = TraceManager.GetMaxMessageLen(codeSection, entry.TraceEventType);

            var messageRaw = entry.Message;
            if (entry.GetMessage != null)
            {
                messageRaw = entry.GetMessage();
            }
            else if (entry.MessageFormat != null)
            {
                messageRaw = string.Format(entry.MessageFormat, entry.MessageArgs);
            }
            var message = codeSection.IsInnerScope ? "... " + messageRaw : messageRaw;
            if (maxMessageLen > 0 && message != null && message.Length > maxMessageLen) { message = message.Substring(0, maxMessageLen.Value - 3) + "..."; }

            var nesting = getNesting(entry);
            string operationID = !string.IsNullOrEmpty(entry.RequestContext?.RequestId) ? entry.RequestContext?.RequestId : "null";
            var now = DateTime.Now.ToString(_timestampFormat);

            var delta = ""; var lastLineDelta = ""; var lastLineDeltaSB = new StringBuilder();
            var deltaPadded = ""; var lastLineDeltaPadded = "";
            var resultString = "";

            lastLineDeltaPadded = lastLineDelta = getLastLineDeltaOptimized(entry, lastWrite); // .PadLeft(5)
            if (lastLineDeltaPadded != null && lastLineDeltaPadded.Length < _deltaPadding) { lastLineDeltaPadded = lastLineDeltaPadded.PadLeft(_deltaPadding); }
            if (lastLineDeltaPadded.Length > _deltaPadding) { _deltaPadding = lastLineDeltaPadded.Length; }

            switch (entry.TraceEventType)
            {
                case TraceEventType.Start:
                    // line = $"[{now}] {processName} {source} {category} {tidpid} - {sourceLevel} - {nesting} {message}"; "[{0}] {1} {2} {3} - {4,-11} - {5} {6}"
                    string section = !string.IsNullOrEmpty(codeSection.Name) ? string.Format(".{0}", codeSection.Name) : null;
                    message = $"{codeSection.T?.Name}.{codeSection.MemberName}{section}() START ";
                    if (codeSection.Payload != null)
                    {
                        var maxPayloadLen = maxMessageLen >= 0 ? (int)maxMessageLen - Min((int)maxMessageLen, message.Length) : -1;
                        var payloadString = $"{codeSection.Payload.GetLogString()}";
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
                    line = string.Format(_traceMessageFormatStart, now, processName, source, category, tidpid, sourceLevel, nesting, message, lastLineDelta, lastLineDeltaPadded, delta, deltaPadded, resultString);
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
                    line = string.Format(traceMessageFormat, now, processName, source, category, tidpid, sourceLevel, nesting, message, lastLineDelta, lastLineDeltaPadded, delta, deltaPadded, resultString);
                    break;
                default: // case TraceEntryType.Message:
                    // line = $"[{now}] {processName} {source} {category} {tidpid} - {sourceLevel} - {nesting}   {message}"; "[{0}] {1} {2} {3} - {4,-11} - {5}   {6}"
                    deltaPadded = delta = ""; // .PadLeft(5)
                    if (deltaPadded != null && deltaPadded.Length < _deltaPadding) { deltaPadded = deltaPadded.PadLeft(_deltaPadding); }
                    if (deltaPadded.Length > _deltaPadding) { _deltaPadding = deltaPadded.Length; }
                    line = string.Format(_traceMessageFormatInformation, now, processName, source, category, tidpid, sourceLevel, nesting, message, lastLineDelta, lastLineDeltaPadded, delta, deltaPadded, resultString);
                    break;
            }

            if (!string.IsNullOrEmpty(_CRReplace) && !entry.DisableCRLFReplace) { line = line?.Replace("\r", _CRReplace); }
            if (!string.IsNullOrEmpty(_LFReplace) && !entry.DisableCRLFReplace) { line = line?.Replace("\n", _LFReplace); }
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
            if (showNestedFlow == false) { return deptString; }

            string nesting = new string(' ', entry.CodeSection != null ? entry.CodeSection.NestingLevel * 2 : 0);
            return $"{deptString}{nesting}";
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
        private EventLogEntryType trace2EventlogEntryType(TraceEventType level)
        {
            if (level.HasFlag(TraceEventType.Critical)) { return EventLogEntryType.Error; }
            if (level.HasFlag(TraceEventType.Error)) { return EventLogEntryType.Error; }
            if (level.HasFlag(TraceEventType.Warning)) { return EventLogEntryType.Warning; }
            return EventLogEntryType.Information;
        }

        #region Min
        int Min(int a, int b) { return a < b ? a : b; }
        #endregion
    }

    public class DelayedEventLogTraceListener : TraceListenerDelayItems, ISupportFilters
    {
        public DelayedEventLogTraceListener() : base()
        {
            TraceListener innerListener = new EventLogTraceListener();
            InnerListener = innerListener;
        }
    }

}
