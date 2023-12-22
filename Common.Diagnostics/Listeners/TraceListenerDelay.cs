#region using
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
#endregion

namespace Common
{
    public class TraceListenerDelay : TraceListener, ISupportFilters, ISupportInnerListener
    {
        #region const
        const int MINDELAY = 100;
        const int DELAY = 200;
        const int FLUSHDELAY = 400;
        #endregion
        #region internal state
        static Stopwatch _stopwatch = TraceManager.Stopwatch;
        Timer _timer = null;
        long _lastTickWriteTimestamp = 0;
        string _messages;
        TraceListener _innerListener;
        #endregion

        #region ISupportFilters
        string ISupportFilters.Filter { get; set; }
        #endregion
        #region ISupportInnerListener
        public TraceListener InnerListener { get; set; }
        #endregion

        #region .ctor
        static TraceListenerDelay() { }
        public TraceListenerDelay()
        {
            _timer = new Timer(new TimerCallback(this._timer_tick), null, 0, FLUSHDELAY);
        }
        public TraceListenerDelay(TraceListener innerListener) : this()
        {
            _innerListener = innerListener;
        }
        #endregion

        public override void Write(string s) { Write((object)s); }
        public override void Write(object o)
        {
            var entry = o is TraceEntry ? (TraceEntry)o : default(TraceEntry);
            var message = o is TraceEntry ? entry.ToString() : o as string;
            var filter = ((ISupportFilters)this).Filter;
            if (!string.IsNullOrEmpty(filter) && message.IndexOf(filter) < 0) { return; }

            if (_stopwatch.ElapsedMilliseconds < _lastTickWriteTimestamp + DELAY) { _messages += message; return; }

            _lastTickWriteTimestamp = _stopwatch.ElapsedMilliseconds;
            var writeMessage = _messages; _messages = null;
            writeMessage += message;
            _innerListener.Write(writeMessage);
        }

        public override void WriteLine(string s) { WriteLine((object)s); }
        public override void WriteLine(object o)
        {
            var enty = o is TraceEntry ? (TraceEntry)o : default(TraceEntry);
            var message = o is TraceEntry ? enty.ToString() : o as string;
            var filter = ((ISupportFilters)this).Filter;
            if (!string.IsNullOrEmpty(filter) && message.IndexOf(filter) < 0) { return; }

            if (_stopwatch.ElapsedMilliseconds < _lastTickWriteTimestamp + DELAY) { _messages += message + Environment.NewLine; return; }

            _lastTickWriteTimestamp = _stopwatch.ElapsedMilliseconds;
            var writeMessage = _messages; _messages = null;
            writeMessage += message + Environment.NewLine;
            _innerListener.Write(writeMessage);
        }

        public override void Flush()
        {
            if (string.IsNullOrEmpty(_messages)) { return; }
            if (_stopwatch.ElapsedMilliseconds < _lastTickWriteTimestamp + MINDELAY) { return; }

            _lastTickWriteTimestamp = _stopwatch.ElapsedMilliseconds;
            var writeMessage = _messages; _messages = null;
            //writeMessage = writeMessage.EndsWith(Environment.NewLine) ? writeMessage.Substring(0, writeMessage.Length - Environment.NewLine.Length) : writeMessage;
            _innerListener.Write(writeMessage);
        }

        private void _timer_tick(object state)
        {
            Flush();
        }
    }
}
